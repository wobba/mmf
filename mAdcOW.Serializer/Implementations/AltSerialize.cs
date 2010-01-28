using System;
using AltSerialize;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Serializer implemented with AltSerialize project - http://www.codeproject.com/KB/cs/AltSerializer.aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AltSerialize<T> : ISerializeDeserialize<T>
    {
        private readonly ByteSerializer _serializer = new ByteSerializer();

        public AltSerialize()
        {
            _serializer.CacheObject(typeof(T));
        }

        #region ISerializeDeserialize<T> Members

        public byte[] ObjectToBytes(T data)
        {
            byte[] bytes = _serializer.Serialize(data);
            return bytes;
        }

        public T BytesToObject( byte[] bytes )
        {
            return (T)_serializer.Deserialize(bytes);
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