/*
 * 
 * Changes(ID#1, Refer Changes.txt File)
 * --------------------------
 * Removed base Interface  ISerializeDeserialize,base abtract class SerializeDeserializeAbstractBase
 * Methods ObjectsToBytes, BytesToObjects, CanSerializeType were removed. Implemented abstract methods 
 * SerializeObjectToBytes and DeSerializeBytesToObjects
 *  
 */  

using System.IO;

namespace mAdcOW.Serializer
{
    public class ProtocolBufferSerializer<T> : SerializeDeserializeAbstractBase<T>
    {
        /// <summary>
        /// Serializes the object of type T and returns 
        /// corresponding Byte[]
        /// </summary>
        /// <typeparam name="T">
        /// U can be of Type T(Class Definition) or IMappedType
        /// </typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public override byte[] SerializeObjectToBytes<T>(T data)
        {
            //ChangeId#1
            MemoryStream byteStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        /// <summary>
        /// Deserializes the object of type T and returns 
        /// corresponding Byte[]
        /// </summary>
        /// <typeparam name="T">
        /// U can be of Type T(Class Definition) or IMappedType
        /// </typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override T DeSerializeBytesToObject<T>(byte[] bytes)
        {
            //ChangeId#1
            //Changed Method Declaration From ISerializeDeserialize.BytesToObjects
            //Retained same Method body
            MemoryStream byteStream = new MemoryStream(bytes);
            if (bytes.Length == 0)
                return default(T);
            return ProtoBuf.Serializer.Deserialize<T>(byteStream);
        }
    }
}
