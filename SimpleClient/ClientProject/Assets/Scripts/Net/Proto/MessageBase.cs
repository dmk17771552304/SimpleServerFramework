using ProtoBuf;
using SimpleServer.Const;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SimpleServer.Proto
{
    public class MessageBase
    {
        public virtual ProtocolEnum ProtocolType { get; set; }

        public static ProtocolEnum DecodeName(byte[] bytes, int offset, out int nameCount)
        {
            nameCount = 0;
            if (offset + 2 > bytes.Length)
            {
                return ProtocolEnum.None;
            }
            short len = (short)((bytes[offset + 1] << 8) | bytes[offset]);

            if (offset + 2 + len > bytes.Length)
            {
                return ProtocolEnum.None;
            }

            nameCount = 2 + len;
            try
            {
                string name = Encoding.UTF8.GetString(bytes, offset + 2, len);
                return (ProtocolEnum)(Enum.Parse(typeof(ProtocolEnum), name));
            }
            catch (Exception ex)
            {
                Debug.LogError($"不存在的协议: {ex.ToString()}");
                return ProtocolEnum.None;
            }
        }

        public static MessageBase DecodeContent(ProtocolEnum proto, byte[] bytes, int offset, int bodyCount)
        {
            if (bodyCount <= 0)
            {
                Debug.LogError($"协议解析出错,数据长度为0");
                return null;
            }

            try
            {
                byte[] newBytes = new byte[bodyCount];
                Array.Copy(bytes, offset, newBytes, 0, bodyCount);
                string secretKey = Consts.SecretKey;
                if (proto == ProtocolEnum.MessageSecret)
                {
                    secretKey = Consts.PublicKey;
                }
                newBytes = AES.AESDecrypt(newBytes, secretKey);
                using (var memory = new MemoryStream(newBytes,0, newBytes.Length))
                {
                    Type t = null;

                    switch (proto)
                    {
                        case ProtocolEnum.None:
                            break;
                        case ProtocolEnum.MessageSecret:
                            t = typeof(MessageSecret);
                            break;
                        case ProtocolEnum.MessagePing:
                            t = typeof(MessagePing);
                            break;
                        case ProtocolEnum.MessageRegister:
                            t = typeof(MessageRegister);
                            break;
                        case ProtocolEnum.MessageLogin:
                            t = typeof(MessageLogin);
                            break;
                        case ProtocolEnum.MessageTest:
                            t = typeof(MessageTest);
                            break;
                        default:
                            break;
                    }
                    return (MessageBase)Serializer.NonGeneric.Deserialize(t, memory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("协议解密出错:" + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// 编码协议名
        /// </summary>
        /// <param name="msgBase"></param>
        /// <returns></returns>
        public static byte[] EncodeName(MessageBase msgBase)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(msgBase.ProtocolType.ToString());
            short len = (short)nameBytes.Length;
            byte[] bytes = new byte[2 + len];
            bytes[0] = (byte)(len % 256);
            bytes[1] = (byte)(len / 256);
            Array.Copy(nameBytes, 0, bytes, 2, len);
            return bytes;
        }

        /// <summary>
        /// 协议序列化及加密
        /// </summary>
        /// <param name="msgBase"></param>
        /// <returns></returns>
        public static byte[] EncodeContent(MessageBase msgBase)
        {
            using (var memory = new MemoryStream())
            {
                //将我们的协议类进行序列化转换成数组
                Serializer.Serialize(memory, msgBase);
                byte[] bytes = memory.ToArray();
                string secret = Consts.SecretKey;
                //对数组进行加密
                if (msgBase is MessageSecret)
                {
                    secret = Consts.PublicKey;
                }
                bytes = AES.AESEncrypt(bytes, secret);
                return bytes;
            }
        }
    }
}
