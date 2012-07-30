using System;
using System.IO;
using System.Xml.Serialization;

namespace mAdcOW.Serializer
{
    public class XmlSerializer<T> : ISerializeDeserialize<T>
    {
        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(T));

        public byte[] ObjectToBytes(T data)
        {
            MemoryStream byteStream = new MemoryStream();
            _serializer.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        public T BytesToObject(byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            return (T)_serializer.Deserialize(byteStream);
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
                DataHelper.AssignEmptyData(ref classInstance);
                byte[] bytes = ObjectToBytes(classInstance);
                BytesToObject(bytes);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
