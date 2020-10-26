using ProtoBuf;
using SimpleServer.Business;

namespace SimpleServer.Proto
{
    [ProtoContract]
    public class MessageSecret : MessageBase
    {
        public MessageSecret()
        {
            ProtocolType = ProtocolEnum.MessageSecret;
        }

        [ProtoMember(1)]
        public override ProtocolEnum ProtocolType { get; set; }

        [ProtoMember(2)]
        public string Srcret;
    }

    [ProtoContract]
    public class MessagePing : MessageBase
    {
        public MessagePing()
        {
            ProtocolType = ProtocolEnum.MessagePing;
        }

        [ProtoMember(1)]
        public override ProtocolEnum ProtocolType { get; set; }
    }

    [ProtoContract]
    public class MessageTest : MessageBase
    {
        public MessageTest()
        {
            ProtocolType = ProtocolEnum.MessageTest;
        }

        [ProtoMember(1)]
        public override ProtocolEnum ProtocolType { get; set; }

        [ProtoMember(2)]
        public string ReqestContent { get; set; }

        [ProtoMember(3)]
        public string ResponseContent { get; set; }
    }
    
    
    [ProtoContract]
    public class MessageRegister : MessageBase
    {
        //每一个协议类必然包含构造函数来确定当前协议类型，并且都有ProtoType进行序列化标记
        public MessageRegister()
        {
            ProtocolType = ProtocolEnum.MessageRegister;
        }

        [ProtoMember(1)]
        public override ProtocolEnum ProtocolType { get; set; }
        //客户端向服务器发送的数据
        [ProtoMember(2)]
        public string Account;
        [ProtoMember(3)]
        public string Password;
        [ProtoMember(4)]
        public string Code;
        [ProtoMember(5)]
        public RegisterType RegisterType;
        //服务器向客户端返回的数据
        [ProtoMember(6)]
        public RegisterResult Result;
    }

    [ProtoContract]
    public class MessageLogin : MessageBase
    {
        //每一个协议类必然包含构造函数来确定当前协议类型，并且都有ProtoType进行序列化标记
        public MessageLogin()
        {
            ProtocolType = ProtocolEnum.MessageLogin;
        }

        [ProtoMember(1)]
        public override ProtocolEnum ProtocolType { get; set; }
        //客户端向服务器发送的数据
        [ProtoMember(2)]
        public string Account;
        [ProtoMember(3)]
        public string Password;
        [ProtoMember(4)]
        public LoginType LoginType;
        //服务器向客户端返回的数据
        [ProtoMember(5)]
        public LoginResult Result;
        [ProtoMember(6)]
        public string Token;
    }
}
