using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Serializer implemented with the System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetSerializer<T> : ISerializeDeserialize<T>
    {
        private readonly BinaryFormatter _formatter = new BinaryFormatter();

        #region ISerializeDeserialize<T> Members

        public byte[] ObjectToBytes(T data)
        {
            MemoryStream byteStream = new MemoryStream();
            _formatter.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        public T BytesToObject(byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            return (T) _formatter.UnsafeDeserialize(byteStream, null);
        }

        public bool CanSerializeType()
        {
            return typeof (T).IsSerializable;
        }

        #endregion
    }
}