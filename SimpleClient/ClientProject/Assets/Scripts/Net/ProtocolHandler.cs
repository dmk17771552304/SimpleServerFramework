using System;
using SimpleServer.Business;
using SimpleServer.Proto;
using UnityEngine;

//所有的协议收发的一个单独类
public class ProtocolHandler
{
    private NetController _netController;
    
    public ProtocolHandler(NetController netController)
    {
        _netController = netController;
    }
    
    public void SocketTest() 
    {
        MessageTest msg = new MessageTest();
        msg.ReqestContent = "Ocean";
        _netController.SendMessage(msg);
        _netController.RegisterProtoHandler(ProtocolEnum.MessageTest, (message) =>
        {
            Debug.Log("测试回调：" + ((MessageTest)message).ResponseContent);
        });
    }

    /// <summary>
    /// 注册协议提交
    /// </summary>
    /// <param name="registerType"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <param name="callback"></param>
    public void Register(RegisterType registerType, string userName, string password, string code, Action<RegisterResult> callback) 
    {
        MessageRegister msg = new MessageRegister();
        msg.RegisterType = registerType;
        msg.Account = userName;
        msg.Password = password;
        msg.Code = code;
        _netController.SendMessage(msg);
        _netController.RegisterProtoHandler(ProtocolEnum.MessageRegister, (message) =>
        {
            MessageRegister msgRegister = (MessageRegister)message;
            callback(msgRegister.Result);
        });
    }

    /// <summary>
    /// 登录协议的提交
    /// </summary>
    /// <param name="loginType"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="callback"></param>
    public void Login(LoginType loginType,string userName,string password, Action<LoginResult,string> callback) 
    {
        MessageLogin msg = new MessageLogin();
        msg.Account = userName;
        msg.Password = password;
        msg.LoginType = loginType;
        _netController.SendMessage(msg);
        _netController.RegisterProtoHandler(ProtocolEnum.MessageLogin,(message)=> 
        {
            MessageLogin msgLogin = (MessageLogin)message;
            callback(msgLogin.Result, msgLogin.Token);
        });
    }
}
