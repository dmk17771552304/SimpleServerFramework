using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using SimpleServer.Const;
using SimpleServer.Net;
using SimpleServer.Proto;
using UnityEngine;

public delegate void OnNetEventCallback(string str);
public delegate void OnProtocolHandlerCallback(MessageBase msg);

public enum NetEvent 
{
    ConnectSuccess = 1,
    ConnectFail = 2,
    Close = 3,
}

public class NetController 
{
    private string _secretKey;
    private Socket _socket;

    private ByteArray _readBuffer;

    private string _ip;
    private int _port;

    //链接状态
    private bool _connecting = false;
    private bool _closing = false;

    private Thread _msgThread;
    private Thread _heartThread;

    private long _lastPingTime;
    private long _lastPongTime;

    private Queue<ByteArray> _writeQueue;

    private List<MessageBase> _messageList;
    private List<MessageBase> _unityMessageList;
    //消息列表长度
    private int _messageCount = 0;

    private Dictionary<NetEvent, OnNetEventCallback> _netListeners = new Dictionary<NetEvent, OnNetEventCallback>();
    private Dictionary<ProtocolEnum, OnProtocolHandlerCallback> _protoHandlers = new Dictionary<ProtocolEnum, OnProtocolHandlerCallback>();

    private bool _disConnected = false;
    //是否链接成功过（只要链接成功过就是true，再也不会变成false）
    private bool _isConnectSuccessed = false;
    private bool _reConnect = false;

    private NetworkReachability _curNetWork = NetworkReachability.NotReachable;

    private MessageParser _messageParser;

    public NetController(MessageParser messageParser)
    {
        _messageParser = messageParser;
    }

    public IEnumerator CheckNet()
    {
        _curNetWork = Application.internetReachability;
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (_isConnectSuccessed)
            {
                if (_curNetWork != Application.internetReachability)
                {
                    ReConnect();
                    _curNetWork = Application.internetReachability;
                }
            }
        }
    }

    /// <summary>
    /// 监听链接事件
    /// </summary>
    /// <param name="netEvent"></param>
    /// <param name="callback"></param>
    public void RegisterNetEventListener(NetEvent netEvent, OnNetEventCallback callback) 
    {
        if (_netListeners.ContainsKey(netEvent))
        {
            _netListeners[netEvent] += callback;
        }
        else 
        {
            _netListeners[netEvent] = callback;
        }
    }

    public void UnRegisterNetEventListener(NetEvent netEvent, OnNetEventCallback callback)
    {
        if (_netListeners.ContainsKey(netEvent))
        {
            _netListeners[netEvent] -= callback;
            if (_netListeners[netEvent] == null)
            {
                _netListeners.Remove(netEvent);
            }
        }
    }

    void NotifyNetEvent(NetEvent netEvent, string notifyContent) 
    {
        if (_netListeners.ContainsKey(netEvent)) 
        {
            _netListeners[netEvent](notifyContent);
        }
    }

    /// <summary>
    /// 一个协议希望只有一个监听
    /// </summary>
    /// <param name="protocolEnum"></param>
    /// <param name="listener"></param>
    public void RegisterProtoHandler(ProtocolEnum protocolEnum, OnProtocolHandlerCallback listener)
    {
        _protoHandlers[protocolEnum] = listener;
    }

    private void NotifyProtoHandler(ProtocolEnum protocolEnum,MessageBase msgBase) 
    {
        if (_protoHandlers.ContainsKey(protocolEnum))
        {
            _protoHandlers[protocolEnum](msgBase);
        }
    }

    public void Update() 
    {
        if (_disConnected && _isConnectSuccessed) 
        {
            //弹框，确定是否重连
            //重新链接
            ReConnect();
            //退出游戏
            _disConnected = false;
        }

        //断开链接后，链接服务器之后自动登录
        if (!string.IsNullOrEmpty(_secretKey) && _socket.Connected && _reConnect) 
        {
            //在本地保存了我们的账户和token，然后进行判断有无账户和token，

            //使用token登录
            _reConnect = false;
        }

        MsgUpdate();
    }

    void MsgUpdate()
    {
        if (_socket != null && _socket.Connected)
        {
            if (_messageCount == 0)
            {
                return;
            }
            MessageBase msgBase = null;
            lock (_unityMessageList) 
            {
                if (_unityMessageList.Count > 0) 
                {
                    msgBase = _unityMessageList[0];
                    _unityMessageList.RemoveAt(0);
                    _messageCount--;
                }
            }
            if (msgBase != null)
            {
                NotifyProtoHandler(msgBase.ProtocolType, msgBase);
            }
        }
    }

    void MsgThread()
    {
        while (_socket != null && _socket.Connected)
        {
            MessageBase msgBase = null;
            lock (_messageList)
            {
                if (_messageList.Count <= 0)
                    continue;

                msgBase = _messageList[0];
                _messageList.RemoveAt(0);
            }

            if (msgBase == null)
            {
                break;
            }

            if (msgBase is MessagePing)
            {
                _lastPongTime = GetTimeStamp();
                Debug.Log("收到心跳包！！！！！！！");
                _messageCount--;
            }
            else
            {
                lock (_unityMessageList)
                {
                    _unityMessageList.Add(msgBase);
                }
            }
        }
    }

    void PingThread() 
    {
        while (_socket != null && _socket.Connected)
        {
            long timeNow = GetTimeStamp();
            if (timeNow - _lastPingTime > Consts.PingInterval) 
            {
                MessagePing msgPing = new MessagePing();
                SendMessage(msgPing);
                _lastPingTime = GetTimeStamp();
            }

            //如果心跳包过长时间没收到，就关闭连接
            if (timeNow - _lastPongTime > Consts.PingInterval * 4) 
            {
                Close(false);
            }
        }
    }

    /// <summary>
    /// 重连方法
    /// </summary>
    private void ReConnect() 
    {
        Connect(_ip, _port);
        _reConnect = true;
    }

    /// <summary>
    /// 链接服务器函数
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public void Connect(string ip, int port) 
    {
        if (_socket != null && _socket.Connected) 
        {
            Debug.LogError("链接失败，已经链接了！");
            return;
        }

        if (_connecting)
        {
            Debug.LogError("链接失败，正在链接中！");
            return;
        }
        InitState();
        _socket.NoDelay = true;
        _connecting = true;
        _socket.BeginConnect(ip, port, ConnectCallback, _socket);
        _ip = ip;
        _port = port;
    }

    /// <summary>
    /// 初始化状态
    /// </summary>
    void InitState() 
    {
        //初始化变量
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _readBuffer = new ByteArray();
        _writeQueue = new Queue<ByteArray>();
        _connecting = false;
        _closing = false;
        _messageList = new List<MessageBase>();
        _unityMessageList = new List<MessageBase>();
        _messageCount = 0;
        _lastPingTime = GetTimeStamp();
        _lastPongTime = GetTimeStamp();
    }

    /// <summary>
    /// 链接回调
    /// </summary>
    /// <param name="ar"></param>
    void ConnectCallback(IAsyncResult ar) 
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            NotifyNetEvent(NetEvent.ConnectSuccess, "连接成功");
            _isConnectSuccessed = true;
            _msgThread = new Thread(MsgThread);
            _msgThread.IsBackground = true;
            _msgThread.Start();
            _connecting = false;
            _heartThread = new Thread(PingThread);
            _heartThread.IsBackground = true;
            _heartThread.Start();
            SecretRequest();
            Debug.Log("Socket Connect Success");
            _socket.BeginReceive(_readBuffer.Bytes,_readBuffer.WriteIndex,_readBuffer.Remain,0, ReceiveCallBack, socket);
        }
        catch (SocketException ex) 
        {
            Debug.LogError("Socket Connect fail:" + ex.ToString());
            _connecting = false;
        }
    }

    private void SecretRequest()
    {
        MessageSecret msg = new MessageSecret();
        SendMessage(msg);
        RegisterProtoHandler(ProtocolEnum.MessageSecret, (message) =>
        {
            SetKey(((MessageSecret) message).Secret);
            Debug.Log("获取密钥：" + ((MessageSecret) message).Secret);
        });
    }

    /// <summary>
    /// 接受数据回调
    /// </summary>
    /// <param name="ar"></param>
    void ReceiveCallBack(IAsyncResult ar) 
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndReceive(ar);
            if (count <= 0) 
            {
                Close();
                //关闭链接
                return;
            }

            _readBuffer.WriteIndex += count;
            OnReceiveData();
            if (_readBuffer.Remain < 8)
            {
                _readBuffer.MoveBytesIndex();
                _readBuffer.ReSize(_readBuffer.Length * 2);
            }
            socket.BeginReceive(_readBuffer.Bytes, _readBuffer.WriteIndex, _readBuffer.Remain, 0, ReceiveCallBack, socket);
        }
        catch (SocketException ex)
        {
            Debug.LogError("Socket ReceiveCallBack fail:" + ex.ToString());
            Close();
        }
    }

    /// <summary>
    /// 对数据进行处理
    /// </summary>
    void OnReceiveData() 
    {
        if (_readBuffer.Length <= 4 || _readBuffer.ReadIndex < 0)
            return;

        int readIdx = _readBuffer.ReadIndex;
        byte[] bytes = _readBuffer.Bytes;
        int bodyLength = BitConverter.ToInt32(bytes, readIdx);
        //读取协议长度之后进行判断，如果消息长度小于读出来的消息长度，证明是没有一条完整的数据
        if (_readBuffer.Length < bodyLength + 4) 
        {
            return;
        }

        _readBuffer.ReadIndex += 4;
        int nameCount = 0;
        ProtocolEnum protocol = _messageParser.DecodeName(_readBuffer.Bytes, _readBuffer.ReadIndex, out nameCount);
        if (protocol == ProtocolEnum.None) 
        {
            Debug.LogError("OnReceiveData MsgBase.DecodeName fail");
            Close();
            return;
        }

        _readBuffer.ReadIndex += nameCount;
        //解析协议体
        int bodyCount = bodyLength - nameCount;
        try
        {
            MessageBase msgBase = _messageParser.DecodeContent(protocol, _readBuffer.Bytes, _readBuffer.ReadIndex, bodyCount);
            if (msgBase == null) 
            {
                Debug.LogError("接受数据协议内容解析出错");
                Close();
                return;
            }
            _readBuffer.ReadIndex += bodyCount;
            _readBuffer.CheckMoveBytes();
            //协议具体的操作
            lock (_messageList) 
            {
                _messageList.Add(msgBase);
            }
            _messageCount++;
            //处理粘包
            if (_readBuffer.Length > 4) 
            {
                OnReceiveData();
            }
        }
        catch (Exception ex) 
        {
            Debug.LogError("Socket OnReceiveData error:" + ex.ToString());
            Close();
        }
    }

    /// <summary>
    /// 发送数据到服务器
    /// </summary>
    /// <param name="msgBase"></param>
    public void SendMessage(MessageBase msgBase) 
    {
        if (_socket == null || !_socket.Connected) 
        {
            return;
        }

        if (_connecting) 
        {
            Debug.LogError("正在链接服务器中，无法发送消息！");
            return;
        }

        if (_closing) 
        {
            Debug.LogError("正在关闭链接中，无法发送消息!");
            return;
        }

        try
        {
            byte[] nameBytes = _messageParser.EncodeName(msgBase);
            byte[] bodyBytes = _messageParser.EncodeContent(msgBase);
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] byteHead = BitConverter.GetBytes(len);
            byte[] sendBytes = new byte[byteHead.Length + len];
            Array.Copy(byteHead, 0, sendBytes, 0, byteHead.Length);
            Array.Copy(nameBytes, 0, sendBytes, byteHead.Length, nameBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, byteHead.Length + nameBytes.Length, bodyBytes.Length);
            ByteArray ba = new ByteArray(sendBytes);
            int count = 0;
            lock (_writeQueue) 
            {
                _writeQueue.Enqueue(ba);
                count = _writeQueue.Count;
            }

            if (count == 1) 
            {
                _socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, _socket);
            }
        }
        catch (Exception ex) 
        {
            Debug.LogError("SendMessage error:" + ex.ToString());
            Close();
        }
    }

    /// <summary>
    /// 发送结束回调
    /// </summary>
    /// <param name="ar"></param>
    void SendCallBack(IAsyncResult ar) 
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            if (socket == null || !socket.Connected) return;
            int count = socket.EndSend(ar);
            //判断是否发送完成
            ByteArray ba;
            lock (_writeQueue) 
            {
                ba = _writeQueue.First();
            }
            ba.ReadIndex += count;
            //代表发送完整
            if (ba.Length == 0) 
            {
                lock (_writeQueue) 
                {
                    _writeQueue.Dequeue();
                    if (_writeQueue.Count > 0)
                    {
                        ba = _writeQueue.First();
                    }
                    else 
                    {
                        ba = null;
                    }
                }
            }

            //发送不完整或发送完整且存在第二条数据
            if (ba != null)
            {
                socket.BeginSend(ba.Bytes, ba.ReadIndex, ba.Length, 0, SendCallBack, socket);
            }
            //确保关闭链接前，先把消息发送出去
            else if (_closing) 
            {
                RealClose();
            }
        }
        catch (SocketException ex) 
        {
            Debug.LogError("SendCallBack error:" + ex.ToString());
            Close();
        }
    }

    /// <summary>
    /// 关闭链接
    /// </summary>
    /// <param name="normal"></param>
    public void Close(bool normal = true) 
    {
        if (_socket == null || _connecting) 
        {
            return;
        }

        if (_connecting) return;

        if (_writeQueue.Count > 0)
        {
            _closing = true;
        }
        else 
        {
            RealClose(normal);
        }
    }

    void RealClose(bool normal = true) 
    {
        _secretKey = "";
        _socket.Close();
        NotifyNetEvent(NetEvent.Close, normal ? "正常关闭" : "错误原因关闭");
        _disConnected = true;
        if (_heartThread != null && _heartThread.IsAlive)
        {
            _heartThread.Abort();
            _heartThread = null;
        }
        if (_msgThread != null && _msgThread.IsAlive)
        {
            _msgThread.Abort();
            _msgThread = null;
        }
        Debug.Log("Close Socket");
    }

    private void SetKey(string key) 
    {
        _secretKey = key;
    }

    private static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
}
