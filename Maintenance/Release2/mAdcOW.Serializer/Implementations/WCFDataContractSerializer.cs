using System;
using System.IO;
using System.Runtime.Serialization;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Serializer implemented with the System.Runtime.Serialization.DataContractSerializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WcfDataContractSerializer<T> : ISerializeDeserialize<T>
    {
        private readonly DataContractSerializer _serializer = new DataContractSerializer(typeof (T));

        #region ISerializeDeserialize<T> Members

        public byte[] ObjectToBytes(T data)
        {
            MemoryStream byteStream = new MemoryStream();
            _serializer.WriteObject(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        public T BytesToObject(byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            return (T) _serializer.ReadObject(byteStream);
        }

        public bool CanSerializeType()
        {
            try
            {
                object[] args = null;
                if (typeof(T) == typeof(string))
                {
                    args = new object[] { new[] { 'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't' } };
                }
                T classInstance = (T)Activator.CreateInstance(typeof(T), args);
                byte[] bytes = ObjectToBytes(classInstance);
                BytesToObject(bytes);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}