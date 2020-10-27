using SimpleServer.Business;
using SimpleServer.Const;
using SimpleServer.Proto;

namespace SimpleServer.Net
{
    public class MessageHandler
    {
        private UserController _useController;
        private ServerSocket _serverSocket;

        public MessageHandler(UserController userController, ServerSocket serverSocket)
        {
            _useController = userController;
            _serverSocket = serverSocket;
        }

        public void MessageSecrect(ClientSocket clientSocket, MessageBase messageBase)
        {
            MessageSecret messageSecrect = (MessageSecret)messageBase;
            messageSecrect.Srcret = Consts.SecretKey;
            _serverSocket.Send(clientSocket, messageBase);
        }

        public void MessagePing(ClientSocket clientSocket, MessageBase messageBase)
        {
            clientSocket.LastPingTime = _serverSocket.GetTimeStamp();
            MessagePing messagePong = new MessagePing();
            _serverSocket.Send(clientSocket, messagePong);
        }

        public void MessageTest(ClientSocket clientSocket, MessageBase messageBase)
        {
            MessageTest messageTest = (MessageTest)messageBase;
            Debug.Log(messageTest.ReqestContent);
            messageTest.ResponseContent = "服务器下发的数据";
            _serverSocket.Send(clientSocket, messageTest);
        }

        /// <summary>
        /// 处理注册信息
        /// </summary>
        /// <param name="c"></param>
        /// <param name="msgBase"></param>
        public void MessageRegister(ClientSocket clientSocket, MessageBase msgBase)
        {
            MessageRegister msg = (MessageRegister)msgBase;
            var rst = _useController.Register(msg.RegisterType, msg.Account, msg.Password, out string token);
            msg.Result = rst;
            _serverSocket.Send(clientSocket, msg);
        }

        /// <summary>
        /// 处理登录信息
        /// </summary>
        /// <param name="c"></param>
        /// <param name="msgBase"></param>
        public void MessageLogin(ClientSocket clientSocket, MessageBase msgBase)
        {
            MessageLogin msg = (MessageLogin)msgBase;
            var rst = _useController.Login(msg.LoginType, msg.Account, msg.Password, out int userid, out string token);
            msg.Result = rst;
            msg.Token = token;
            clientSocket.UserId = userid;
            _serverSocket.Send(clientSocket, msg);
        }
    }
}
