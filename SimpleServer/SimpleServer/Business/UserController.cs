using MySql;
using MySql.MySqlData;
using System;

namespace SimpleServer.Business
{
    public class UserController 
    {
        private MySqlController _mySqlController;

        public UserController(MySqlController mySqlController)
        {
            _mySqlController = mySqlController;
        }


        /// <summary>
        /// 注册，正常情况下，还要包含检测验证码是否正确
        /// </summary>
        /// <param name="registerType"></param>
        /// <param name="userName"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public RegisterResult Register(RegisterType registerType, string userName, string pwd, out string token)
        {
            token = "";
            try
            {
                int count = _mySqlController.SqlSugarClient.Queryable<User>().Where(it => it.Username == userName).Count();
                if (count > 0) return RegisterResult.AlreadyExist;
                User user = new User();
                switch (registerType)
                {
                    //不算是手机验证码或者邮箱验证码，再注册之前会有一个协议来申请验证码，申请的验证码生成后在数据库储存一份，然后在注册的时候把客户端传入的验证码与数据库的验证码进行比较，如果不一致，正面注册验证码错误，返回  RegisterResult.WrongCode
                    case RegisterType.Phone:
                        user.Logintype = LoginType.Phone.ToString();
                        break;
                    case RegisterType.Mail:
                        user.Logintype = LoginType.Mail.ToString();
                        break;
                }
                user.Username = userName;
                user.Password = pwd;
                user.Token = Guid.NewGuid().ToString();
                user.Logindate = DateTime.Now;
                token = user.Token;
                _mySqlController.SqlSugarClient.Insertable(user).ExecuteCommand();
                return RegisterResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError("注册失败：" + ex.ToString());
                return RegisterResult.Failed;
            }
        }

        public LoginResult Login(LoginType loginType, string userName, string pwd, out int userid, out string token)
        {
            userid = 0;
            token = "";
            try
            {
                User user = null;
                switch (loginType)
                {
                    case LoginType.Phone:
                    case LoginType.Mail:
                        user = _mySqlController.SqlSugarClient.Queryable<User>().Where(it
                            => it.Username == userName).Single();
                        break;
                    //如果是QQ和微信，在User里面要多存一个Unionid，在这里判断的时候就是it.Unionid == userName
                    case LoginType.QQ:
                    case LoginType.WX:
                        break;
                    case LoginType.Token:
                        user = _mySqlController.SqlSugarClient.Queryable<User>().Where(it
                            => it.Username == userName).Single();
                        break;
                }

                if (user == null)
                {
                    //QQ微信是首次登录的话相当于注册
                    if (loginType == LoginType.QQ || loginType == LoginType.WX)
                    {
                        //在数据库注册QQWX
                        user = new User();
                        user.Username = userName;
                        user.Password = pwd;
                        user.Logintype = loginType.ToString();
                        user.Token = Guid.NewGuid().ToString();
                        user.Logindate = DateTime.Now;
                        //储存Unionid = userName
                        token = user.Token;
                        userid = _mySqlController.SqlSugarClient.Insertable(user).ExecuteReturnIdentity();
                        return LoginResult.Success;
                    }
                    else
                    {
                        return LoginResult.UserNotExist;
                    }
                }
                else
                {
                    if (loginType != LoginType.Token)
                    {
                        if (loginType == LoginType.Phone)
                        {
                            if (user.Password != pwd)
                                return LoginResult.WrongPwd;
                        }
                        else if (loginType == LoginType.Mail)
                        {
                            if (user.Password != pwd)
                                return LoginResult.WrongPwd;
                        }
                    }
                    else
                    {
                        if (user.Token != pwd) return LoginResult.TimeoutToken;
                    }
                    user.Token = Guid.NewGuid().ToString();
                    user.Logindate = DateTime.Now;
                    token = user.Token;
                    _mySqlController.SqlSugarClient.Updateable(user).ExecuteCommand();
                    userid = user.Id;
                    return LoginResult.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("登录失败：" + ex.ToString());
                return LoginResult.Failed;
            }
        }
    }
}
