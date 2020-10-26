using MySql;
using SimpleServer.Business;
using SimpleServer.Net;
using SimpleServer.Proto;
using System;

namespace SimpleServer
{
    class Program
    {
        private static MySqlController _mySqlController;
        private static ServerSocket _serverSocket;
        private static MessageHandler _messageHandler;
        private static UserController _useController;
        private static MessageParser _messageParser;

        static void Main(string[] args)
        {
            Console.WriteLine("The Server Started!");

            Init();

            _mySqlController.Init();
            _serverSocket.Init();

            Console.ReadLine();
        }

        private static void Init()
        {
            _mySqlController = new MySqlController();
            _useController = new UserController(_mySqlController);

            _messageParser = new MessageParser();

            _messageHandler = new MessageHandler(_useController);

            _serverSocket = new ServerSocket(_messageParser);

            _messageHandler.InitServerSocket(_serverSocket);

            _messageParser.RegisterProtocol(ProtocolEnum.MessageSecret, typeof(MessageSecret));
            _messageParser.RegisterProtocol(ProtocolEnum.MessagePing, typeof(MessagePing));
            _messageParser.RegisterProtocol(ProtocolEnum.MessageRegister, typeof(MessageRegister));
            _messageParser.RegisterProtocol(ProtocolEnum.MessageLogin, typeof(MessageLogin));
            _messageParser.RegisterProtocol(ProtocolEnum.MessageTest, typeof(MessageTest));

            _serverSocket.RegisterProtocol(ProtocolEnum.MessageSecret, (clientSocket, messageBase) =>
            {
                _messageHandler.MessageSecrect(clientSocket, messageBase);
            });
            _serverSocket.RegisterProtocol(ProtocolEnum.MessagePing, (clientSocket, messageBase) =>
            {
                _messageHandler.MessagePing(clientSocket, messageBase);
            });
            _serverSocket.RegisterProtocol(ProtocolEnum.MessageRegister, (clientSocket, messageBase) =>
            {
                _messageHandler.MessageRegister(clientSocket, messageBase);
            });
            _serverSocket.RegisterProtocol(ProtocolEnum.MessageLogin, (clientSocket, messageBase) =>
            {
                _messageHandler.MessageLogin(clientSocket, messageBase);
            });
            _serverSocket.RegisterProtocol(ProtocolEnum.MessageTest, (clientSocket, messageBase) =>
            {
                _messageHandler.MessageTest(clientSocket, messageBase);
            });
        }
    }
}
