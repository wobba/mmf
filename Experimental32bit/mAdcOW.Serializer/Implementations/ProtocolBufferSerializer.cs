using System;
using System.IO;

namespace mAdcOW.Serializer.Implementations
{
    public class ProtocolBufferSerializer<T> : ISerializeDeserialize<T>
    {
        public byte[] ObjectToBytes(T data)
        {
            MemoryStream byteStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        public T BytesToObject(byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.Deserialize<T>(byteStream);
        }

        public bool CanSerializeType()
        {
            try
            {
                object[] args = null;
                if (typeof (T) == typeof (string))
                {
                    args = new object[] {new[] {'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't'}};
                }
                T classInstance = (T) Activator.CreateInstance(typeof (T), args);
                byte[] bytes = ObjectToBytes(classInstance);
                if (bytes.Length == 0) return false;
                BytesToObject(bytes);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }
}