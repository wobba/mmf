/*
 * 
 * 
 * Changes(ID#1, Refer Changes.txt File)
 * --------------------------
 * Removed base Interface  ISerializeDeserialize,base abtract class SerializeDeserializeAbstractBase
 * Methods ObjectsToBytes, BytesToObjects, CanSerializeType were removed. Implemented abstract methods 
 * SerializeObjectToBytes and DeSerializeBytesToObjects
 *  
 */                                                       
 
using System.IO;
using System.Runtime.Serialization;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Serializer implemented with the System.Runtime.Serialization.DataContractSerializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    
    public class WcfDataContractSerializer<T> : SerializeDeserializeAbstractBase<T>
    {
        DataContractSerializer _serializer;
        
        #region SerializeDeserializeAbstractBase<T> Members

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
            GetSerializer<U>().WriteObject(byteStream, data);
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
            return (U) GetSerializer<U>().ReadObject(byteStream);
        }

        
        private DataContractSerializer GetSerializer<U>()
        {
            _serializer = new DataContractSerializer(typeof (U));
            return _serializer;
        }
        #endregion
    }
}