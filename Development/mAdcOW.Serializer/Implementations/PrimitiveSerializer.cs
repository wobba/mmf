using System;

namespace mAdcOW.Serializer
{
    public class PrimitiveSerializerByte : ISerializeDeserialize<byte>
    {
        #region ISerializeDeserialize<byte> Members

        public byte[] ObjectToBytes(byte data)
        {
            byte[] buffer = new byte[1];
            buffer[0] = data;
            return buffer;
        }

        public byte BytesToObject(byte[] bytes)
        {
            return bytes[0];
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerBool : ISerializeDeserialize<bool>
    {
        #region ISerializeDeserialize<bool> Members

        public byte[] ObjectToBytes(bool data)
        {
            return BitConverter.GetBytes(data);
        }

        public bool BytesToObject(byte[] bytes)
        {
            return BitConverter.ToBoolean(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerChar : ISerializeDeserialize<char>
    {
        #region ISerializeDeserialize<char> Members

        public byte[] ObjectToBytes(char data)
        {
            return BitConverter.GetBytes(data);
        }

        public char BytesToObject(byte[] bytes)
        {
            return BitConverter.ToChar(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerDouble : ISerializeDeserialize<double>
    {
        #region ISerializeDeserialize<double> Members

        public byte[] ObjectToBytes(double data)
        {
            return BitConverter.GetBytes(data);
        }

        public double BytesToObject(byte[] bytes)
        {
            return BitConverter.ToDouble(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerFloat : ISerializeDeserialize<float>
    {
        #region ISerializeDeserialize<float> Members

        public byte[] ObjectToBytes(float data)
        {
            return BitConverter.GetBytes(data);
        }

        public float BytesToObject(byte[] bytes)
        {
            return BitConverter.ToSingle(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerInt : ISerializeDeserialize<int>
    {
        #region ISerializeDeserialize<int> Members

        public byte[] ObjectToBytes(int data)
        {
            return BitConverter.GetBytes(data);
        }

        public int BytesToObject(byte[] bytes)
        {
            return BitConverter.ToInt32(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerLong : ISerializeDeserialize<long>
    {
        #region ISerializeDeserialize<long> Members

        public byte[] ObjectToBytes(long data)
        {
            return BitConverter.GetBytes(data);
        }

        public long BytesToObject(byte[] bytes)
        {
            return BitConverter.ToInt64(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerShort : ISerializeDeserialize<short>
    {
        #region ISerializeDeserialize<short> Members

        public byte[] ObjectToBytes(short data)
        {
            return BitConverter.GetBytes(data);
        }

        public short BytesToObject(byte[] bytes)
        {
            return BitConverter.ToInt16(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerUint : ISerializeDeserialize<uint>
    {
        #region ISerializeDeserialize<uint> Members

        public byte[] ObjectToBytes(uint data)
        {
            return BitConverter.GetBytes(data);
        }

        public uint BytesToObject(byte[] bytes)
        {
            return BitConverter.ToUInt32(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerUlong : ISerializeDeserialize<ulong>
    {
        #region ISerializeDeserialize<ulong> Members

        public byte[] ObjectToBytes(ulong data)
        {
            return BitConverter.GetBytes(data);
        }

        public ulong BytesToObject(byte[] bytes)
        {
            return BitConverter.ToUInt64(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }

    public class PrimitiveSerializerUshort : ISerializeDeserialize<ushort>
    {
        #region ISerializeDeserialize<ushort> Members

        public byte[] ObjectToBytes(ushort data)
        {
            return BitConverter.GetBytes(data);
        }

        public ushort BytesToObject(byte[] bytes)
        {
            return BitConverter.ToUInt16(bytes, 0);
        }

        public bool CanSerializeType()
        {
            return true;
        }

        #endregion
    }
}