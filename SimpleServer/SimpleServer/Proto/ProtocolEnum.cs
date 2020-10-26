namespace SimpleServer.Proto
{
    public enum ProtocolEnum
    {
        None = 0,
        MessageSecret =1,
        MessagePing = 2,
        MessageRegister = 3,
        MessageLogin = 4,

        MessageTest = 9999,
    }
}
