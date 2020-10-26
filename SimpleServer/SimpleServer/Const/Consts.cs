namespace SimpleServer.Const
{
    public class Consts
    {
        public static string PublicKey = "OceanSever";

        public static string SecretKey = "Ocean_Up&&NB!!";

#if DEBUG
        public static string IpStr = "127.0.0.1";
#else
        //对应阿里云或腾讯云的 本地ip地址（不是公共ip地址）
        public static string IpStr = "172.45.756.54";
#endif
        public static int Port = 8011;

        public static long PingInterval = 30;

        public static int Default_Byte_Size = 1024;
    }
}
