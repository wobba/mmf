using System;

namespace mAdcOW.Serializer
{
    public class SilverlightSerializer<T> : ISerializeDeserialize<T>
    {
        public byte[] ObjectToBytes(T data)
        {
            return Serialization.SilverlightSerializer.Serialize(data);
        }

        public T BytesToObject(byte[] bytes)
        {
            return (T)Serialization.SilverlightSerializer.Deserialize(bytes);
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
