using SimpleServer.Const;
using SimpleServer.Proto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SimpleServer.Net
{
    public class ServerSocket
    {
        private Socket _listenSocket;

        //临时保存所有socket的集合
        private List<Socket> _handleSockets = new List<Socket>();

        //所有客户端的一个字典
        private Dictionary<Socket, ClientSocket> _clientDict = new Dictionary<Socket, ClientSocket>();

        private List<ClientSocket> _tempList = new List<ClientSocket>();

        private MessageParser _messageParser;

        private Dictionary<ProtocolEnum, Action<ClientSocket, MessageBase>> _protocolHandles = new Dictionary<ProtocolEnum, Action<ClientSocket, MessageBase>>();

        public ServerSocket(MessageParser messageParser)
        {
            _messageParser = messageParser;
        }

        public void RegisterProtocol(ProtocolEnum protocol, Action<ClientSocket, MessageBase> handler)
        {
            _protocolHandles[protocol] = handler;
        }

        public void Init()
        {
            IPAddress ipAddress = IPAddress.Parse(Consts.IpStr);
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, Consts.Port);

            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(iPEndPoint);
            _listenSocket.Listen(50000);

            Debug.Log($"服务器启动监听{_listenSocket.LocalEndPoint.ToString()}");

            while (true)
            {
                CheckAnyHandleSocket();

                try
                {
                    Socket.Select(_handleSockets, null, null, 1000);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e}");
                }

                for (int i = _handleSockets.Count-1; i >=0 ; i--)
                {
                    Socket s = _handleSockets[i];
                    if (s == _listenSocket)
                    {
                        //有客户端链接上来了
                        OnClientConnected(s);
                    }
                    else
                    {
                        //有信息可读
                        OnCliendReceivedBytes(s);
                    }
                }

                //检测心跳包超时的计算
                long timeNow = GetTimeStamp();
                _tempList.Clear();
                foreach (var item in _clientDict.Values)
                {
                    if (timeNow - item.LastPingTime > Consts.PingInterval * 4)
                    {
                        Debug.Log($"Ping Close {item.Socket.RemoteEndPoint}");
                        _tempList.Add(item);
                    }
                }

                foreach (var item in _tempList)
                {
                    CloseClient(item);
                }
            }
        }

        private void CheckAnyHandleSocket()
        {
            _handleSockets.Clear();
            _handleSockets.Add(_listenSocket);

            foreach (var item in _clientDict.Keys)
            {
                _handleSockets.Add(item);
            }
        }

        private void OnClientConnected(Socket listenSocket)
        {
            try
            {
                Socket socket = listenSocket.Accept();
                ClientSocket clientSocket = new ClientSocket();
                clientSocket.Socket = socket;
                clientSocket.LastPingTime = GetTimeStamp();
                _clientDict.Add(socket, clientSocket);
                Debug.Log($"一个客户端连接：{socket.LocalEndPoint.ToString()}，当前{_clientDict.Count}个客户端在线！");
            }
            catch (SocketException e)
            {
                Debug.LogError($"Accept fali:{e}");
            }
        }

        private void OnCliendReceivedBytes(Socket socket)
        {
            ClientSocket clientSocket = _clientDict[socket];
            ByteArray readBuffer = clientSocket.ReadBuffer;

            int count = 0;
            if (readBuffer.Remain <= 0)
            {
                OnReceivedDatas(clientSocket);

                readBuffer.CheckMoveBytes();

                while (readBuffer.Remain <= 0)
                {
                    int expandSize = readBuffer.Length < Consts.Default_Byte_Size ? Consts.Default_Byte_Size : readBuffer.Length;
                    readBuffer.ReSize(expandSize * 2);
                }
            }

            try
            {
                count = socket.Receive(readBuffer.Bytes, readBuffer.WriteIndex, readBuffer.Remain, 0);
            }
            catch (SocketException e)
            { 
                Debug.LogError("Receive fali:" + e);
                CloseClient(clientSocket);
                return;
            }

            //代表客户端断开链接了
            if (count <= 0)
            {
                CloseClient(clientSocket);
                return;
            }

            readBuffer.WriteIndex += count;
            //解析我们的信息
            OnReceivedDatas(clientSocket);
            readBuffer.CheckMoveBytes();
        }

        public long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        private void CloseClient(ClientSocket clientSocket)
        {
            clientSocket.Socket.Close();
            _clientDict.Remove(clientSocket.Socket);
            Debug.Log($"一个客户端断开了连接，当前总连接数：{_clientDict.Count}");
        }


        private void OnReceivedDatas(ClientSocket clientSocket)
        {
            ByteArray readBuffer = clientSocket.ReadBuffer;

            if (readBuffer.Length <= 4 || readBuffer.ReadIndex < 0)
            {
                return;
            }

            int readIdx = readBuffer.ReadIndex;
            byte[] bytes = readBuffer.Bytes;
            int bodyLength = BitConverter.ToInt32(bytes, readIdx);
            //判断接收到的信息长度是否小于包体长度+包体头长度，如果小于，代表我们的信息不全，大于代表信息全了（有可能有粘包存在）
            if (readBuffer.Length < bodyLength + 4)
            {
                return;
            }

            readBuffer.ReadIndex += 4;

            //解析协议名
            int nameCount = 0;
            ProtocolEnum proto = ProtocolEnum.None;
            try
            {
                proto = _messageParser.DecodeName(readBuffer.Bytes, readBuffer.ReadIndex, out nameCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析协议名出错：{ex}");
                CloseClient(clientSocket);
                return;
            }

            if (proto == ProtocolEnum.None)
            {
                Debug.LogError($"OnReceiveData MsgBase.DecodeName  fail.Proto type is Null!");
                CloseClient(clientSocket);
                return;
            }

            readBuffer.ReadIndex += nameCount;

            //解析协议体
            int bodyCount = bodyLength - nameCount;
            MessageBase messageBase = null;

            try
            {
                messageBase = _messageParser.DecodeContent(proto, readBuffer.Bytes, readBuffer.ReadIndex, bodyCount);
                if (messageBase == null)
                {
                    Debug.LogError($"{proto}协议内容解析出错!!!!!");
                    CloseClient(clientSocket);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"接收数据协议内容解析错误：{ex}");
                CloseClient(clientSocket);
                return;
            }

            readBuffer.ReadIndex += bodyCount;
            readBuffer.CheckMoveBytes();

            if (!_protocolHandles.ContainsKey(proto))
            {
                Debug.LogError($"没有协议的处理方法：{proto}");
                CloseClient(clientSocket);
                return;
            }

            _protocolHandles[proto]?.Invoke(clientSocket, messageBase);

            if (readBuffer.Length > 4)
            {
                OnReceivedDatas(clientSocket);
            }
        }

        public void Send(ClientSocket clientSocket, MessageBase messageBase)
        {
            if (clientSocket == null || !clientSocket.Socket.Connected)
            {
                return;
            }

            try
            {
                //分为三部分，头：总协议长度；名字；协议内容。
                byte[] nameBytes = _messageParser.EncodeName(messageBase);
                byte[] bodyBytes = _messageParser.EncodeContent(messageBase);
                int len = nameBytes.Length + bodyBytes.Length;
                byte[] byteHead = BitConverter.GetBytes(len);
                byte[] sendBytes = new byte[byteHead.Length + len];
                Array.Copy(byteHead, 0, sendBytes, 0, byteHead.Length);
                Array.Copy(nameBytes, 0, sendBytes, byteHead.Length, nameBytes.Length);
                Array.Copy(bodyBytes, 0, sendBytes, byteHead.Length + nameBytes.Length, bodyBytes.Length);
                try
                {
                    clientSocket.Socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
                }
                catch (SocketException ex)
                {
                    Debug.LogError("Socket BeginSend Error：" + ex);
                }
            }
            catch (SocketException ex)
            {
                Debug.LogError("Socket发送数据失败：" + ex);
            }
        }
    }
}
