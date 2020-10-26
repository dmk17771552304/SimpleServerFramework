using System;
using SimpleServer.Const;

namespace SimpleServer.Net
{
    public class ByteArray
    {
        private int _initSize = 0;

        private byte[] _bytes;

        public int ReadIndex = 0;
        public int WriteIndex = 0;

        private int _capacity = 0;

        public int Remain { get { return _capacity - WriteIndex; } }

        public int Length { get { return WriteIndex - ReadIndex; } }

        public byte[] Bytes { get { return _bytes; } }

        public ByteArray(byte[] dafalutBytes) 
        {
            _bytes = dafalutBytes;
            _capacity = dafalutBytes.Length;
            _initSize = dafalutBytes.Length;
            ReadIndex = 0;
            WriteIndex = dafalutBytes.Length;
        }
        
        public ByteArray()
        {
            _bytes = new byte[Consts.Default_Byte_Size];
            _capacity = Consts.Default_Byte_Size;
            _initSize = Consts.Default_Byte_Size;
            ReadIndex = 0;
            WriteIndex = 0;
        }

        public void CheckMoveBytes()
        {
            if (Length < 8)
            {
                MoveBytesIndex();
            }
        }

        public void MoveBytesIndex()
        {
            if (ReadIndex < 0)
            {
                return;
            }

            Array.Copy(_bytes, ReadIndex, _bytes, 0, Length);
            WriteIndex = Length;
            ReadIndex = 0;
        }

        public void ReSize(int size)
        {
            if (ReadIndex < 0)
            {
                return;
            }

            if (size < Length)
            {
                return;
            }

            if (size < _initSize)
            {
                return;
            }
               
            int n = 1024;
            while (n < size)
            {
                n *= 2;
            }
            _capacity = n;
            byte[] newBytes = new byte[_capacity];
            Array.Copy(_bytes, ReadIndex, newBytes, 0, Length);
            _bytes = newBytes;
            WriteIndex = Length;
            ReadIndex = 0;
        }
    }
}