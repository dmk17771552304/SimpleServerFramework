﻿using ProtoBuf;
using SimpleServer.Const;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleServer.Proto
{
    public class MessageParser
    {
        private readonly Dictionary<ProtocolEnum, Type> _protocolTypeDict = new Dictionary<ProtocolEnum, Type>(); 

        public void RegisterProtocol(ProtocolEnum protocol,Type t)
        {
            _protocolTypeDict[protocol] = t;
        }

        public ProtocolEnum DecodeName(byte[] bytes, int offset, out int nameCount)
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

        public MessageBase DecodeContent(ProtocolEnum proto, byte[] bytes, int offset, int bodyCount)
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
                using (var memory = new MemoryStream(newBytes, 0, newBytes.Length))
                {
                    if (!_protocolTypeDict.ContainsKey(proto))
                    {
                        Debug.LogError($"无该协议解析:{proto}");
                        return null;
                    }
                    Type t = _protocolTypeDict[proto];

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
        public byte[] EncodeName(MessageBase msgBase)
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
        public byte[] EncodeContent(MessageBase msgBase)
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

    public class MessageBase
    {
        public virtual ProtocolEnum ProtocolType { get; set; }
    }
}
