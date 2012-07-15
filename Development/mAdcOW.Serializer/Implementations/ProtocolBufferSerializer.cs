/*
 * 
 * 
 * 
 * Changes(ID#1, Refer Changes.txt File)
 * --------------------------
 * Removed base Interface  ISerializeDeserialize,base abtract class SerializeDeserializeAbstractBase
 * Methods ObjectsToBytes, BytesToObjects, CanSerializeType were removed. Implemented abstract methods 
 * SerializeObjectToBytes and DeSerializeBytesToObjects
 *  
 */  

using System;
using System.IO;

namespace mAdcOW.Serializer.Implementations
{
    public class ProtocolBufferSerializer<T> : SerializeDeserializeAbstractBase<T>
    {
        /// <summary>
        /// Serializes the object of type U  and returns 
        /// corresponding Byte[]
        /// </summary>
        /// <typeparam name="U">
        /// U can be of Type T(Class Definition) or IMappedType
        /// </typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public override byte[] SerializeObjectToBytes<U>(U data)
        {
            //ChangeId#1
            //Changed Method Declaration From ISerializeDeserialize.ObjectToBytes
            //Retained same Method body
            MemoryStream byteStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        /// <summary>
        /// Deserializes the object of type U  and returns 
        /// corresponding Byte[]
        /// </summary>
        /// <typeparam name="U">
        /// U can be of Type T(Class Definition) or IMappedType
        /// </typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override U DeSerializeBytesToObject<U>(byte[] bytes)
        {
            //ChangeId#1
            //Changed Method Declaration From ISerializeDeserialize.BytesToObjects
            //Retained same Method body
            MemoryStream byteStream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.Deserialize<U>(byteStream);
        }

        //Moved to SerializeDeserializeAbstractBase as a part of ChangeID#1
        //public override bool CanSerializeType()
        //{
        //    try
        //    {
        //        object[] args = null;
        //        if (typeof(T) == typeof(string))
        //        {
        //            args = new object[] { new[] { 'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't' } };
        //        }
        //        T classInstance = (T)Activator.CreateInstance(typeof(T), args);
        //        byte[] bytes = ObjectToBytes(classInstance);
        //        if (bytes.Length == 0) return false;
        //        BytesToObject(bytes);
        //    }
        //    catch (Exception e)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        
    }
}