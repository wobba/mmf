namespace mAdcOW.Serializer
{
    public interface ISerializeDeserialize<T>
    {
        byte[]  ObjectToBytes(T data);
        T BytesToObject(byte[] bytes);
        bool CanSerializeType();
    }
}