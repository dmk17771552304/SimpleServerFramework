using SimpleServer.Business;
using SimpleServer.Proto;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private NetController _netController;
    private ProtocolHandler _protocolHandler;
    private MessageParser _messageParser;

    void Start()
    {
        _messageParser = new MessageParser();
        _messageParser.RegisterProtocol(ProtocolEnum.MessageSecret, typeof(MessageSecret));
        _messageParser.RegisterProtocol(ProtocolEnum.MessagePing, typeof(MessagePing));
        _messageParser.RegisterProtocol(ProtocolEnum.MessageRegister, typeof(MessageRegister));
        _messageParser.RegisterProtocol(ProtocolEnum.MessageLogin, typeof(MessageLogin));
        _messageParser.RegisterProtocol(ProtocolEnum.MessageTest, typeof(MessageTest));
        
        _netController = new NetController(_messageParser);
        _protocolHandler = new ProtocolHandler(_netController);
        
        _netController.RegisterNetEventListener(NetEvent.ConnectSuccess, Debug.Log);
        _netController.RegisterNetEventListener(NetEvent.Close, Debug.Log);
        
        _netController.Connect("127.0.0.1", 8011);
        StartCoroutine(_netController.CheckNet());
    }

    void Update()
    {
        _netController.Update();

        if (Input.GetKeyDown(KeyCode.A))
        {
            _protocolHandler.SocketTest();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _protocolHandler.Register(RegisterType.Phone, "dmk", "dmk", "123456", (res) =>
            {
                if (res == RegisterResult.AlreadyExist)
                {
                    Debug.LogError("该手机号已经注册过了");
                }
                else if (res == RegisterResult.WrongCode)
                {
                    Debug.LogError("验证码错误");
                }
                else if (res == RegisterResult.Forbidden)
                {
                    Debug.LogError("改账户禁止铸错，联系客服！");
                }
                else if (res == RegisterResult.Success)
                {
                    Debug.Log("注册成功");
                }
            });
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            _protocolHandler.Login(LoginType.Phone, "dmk", "dmk", (res, resToken) =>
            {
                if (res == LoginResult.Success)
                {
                    Debug.Log("登录成功");
                }
                else if (res == LoginResult.Failed)
                {
                    Debug.LogError("登录失败");
                }
                else if (res == LoginResult.WrongPwd)
                {
                    Debug.LogError("密码错误");
                }
                else if (res == LoginResult.UserNotExist)
                {
                    Debug.LogError("用户不存在");
                }
            });
        }
    }

    private void OnApplicationQuit()
    {
        _netController.Close();
    }
}
