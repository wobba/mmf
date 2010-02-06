using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.IO;

namespace AltSerialize
{
    public partial class AltSerializer
    {
        #region Deserialization

        // Deserializes value types.
        private object DeserializeValueType(Type objectType)
        {
            if (objectType.IsGenericType)
            {
                // Nullable type?
                Type genericTypeDef = objectType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Nullable<>))
                {
                    objectType = objectType.GetGenericArguments()[0];
                }
                SerializedObjectFlags flags = ReadSerializationFlags();
                if (flags == SerializedObjectFlags.IsNull)
                {
                    return null;
                }
            }
            if (objectType.IsPrimitive)
            {
                int size = Marshal.SizeOf(objectType);
                if (objectType == typeof(char))
                {
                    // Marshal.SizeOf is incorrect for char!  it gives 1, not 2.
                    size = 2;
                }
                if (objectType == typeof(bool)) size = 1;
                byte[] bytes = new byte[size];
                this.Stream.Read(bytes, 0, size);
                return ReadBytes(bytes, objectType);
            }
            else if (objectType == typeof(DateTime))
            {
                long readLong = ReadInt64();
#if MONO
                return new DateTime(readLong);
#else
                return DateTime.FromBinary(readLong);
#endif
            }
            else if (objectType == typeof(TimeSpan))
            {
                long readLong = ReadInt64();
                return TimeSpan.FromTicks(readLong);
            }
            else if (objectType == typeof(Decimal))
            {
                int[] bits = new int[4];
                bits[0] = ReadInt32();
                bits[1] = ReadInt32();
                bits[2] = ReadInt32();
                bits[3] = ReadInt32();
                return new Decimal(bits);
            }
            else if (objectType == typeof(Guid))
            {
                byte[] guidArray = new byte[16];
                this.Stream.Read(guidArray, 0, 16);
                return new Guid(guidArray);
            }
            else if (objectType.IsEnum)
            {
                Type enumType = Enum.GetUnderlyingType(objectType);
                object realObject = Deserialize(enumType);
                return Enum.ToObject(objectType, realObject);
            }
            else
            {
                // Use marshaling to get the bytes of the value type
                int size = Marshal.SizeOf(objectType);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                byte [] bytes = ReadBytes(size);
                Marshal.Copy(bytes, 0, ptr, size);
                object returnObject = Marshal.PtrToStructure(ptr, objectType);
                Marshal.FreeHGlobal(ptr);
                return returnObject;
            }            
        }

        // Quickly deserializes a byte array.
        private object DeserializeByteArray()
        {
            int count = ReadInt32();
            byte[] byteArray = new byte[count];
            ReadBytes(byteArray, 0, count);
            return byteArray;
        }

        // Quickly deserializes a value type array.
        private object DeserializeValueTypeArray(Type baseType)
        {
            int size = Marshal.SizeOf(baseType);
            if (baseType == typeof(char)) size = 2;
            else if (baseType == typeof(bool)) size = 1;

            int count = ReadInt32();
            int ptr = 0;


            Array valueTypeArray = Array.CreateInstance(baseType, count);
            // Read blocks to avoid many allocations.
            int length = count * size;
            while (ptr < length)
            {
                int totalBytes = ptr + BlockSize;
                if (totalBytes > length)
                {
                    totalBytes = length - ptr;
                }
                Stream.Read(arrayBuffer, 0, totalBytes);
                Buffer.BlockCopy(arrayBuffer, 0, valueTypeArray, ptr, totalBytes);
                ptr += totalBytes;
            }
            return valueTypeArray;
        }

        // Deserializes an array.
        private object DeserializeArray(Type objectType, int cacheID)
        {
            int arrayRank = ReadByte();
            Type baseType = objectType.GetElementType();

            if (baseType.IsPrimitive && arrayRank == 1)
            {
                // Single value type arrays can be done quick
                if (baseType == typeof(byte))
                {
                    return DeserializeByteArray();
                }
                else
                {
                    return DeserializeValueTypeArray(baseType);
                }
            }

            int[] maxIndices = new int[arrayRank];
            int length = 0;
            for (int i = 0; i < arrayRank; i++)
            {
                maxIndices[i] = ReadInt32();
                length += maxIndices[i];
            }

            Array newArray = Array.CreateInstance(baseType, maxIndices);

            // This object might be referenced by other objects
            // that it loads, so we need to cache it first!
            if (cacheID > 0)
            {
                Cache.SetCachedObjectId(newArray, cacheID);
            }

            int[] indices = new int[arrayRank];
            for (int i = 0; i < length; i++)
            {
                newArray.SetValue(DeserializeElement(baseType), indices);
                //newArray.SetValue(Deserialize(), indices);

                indices[indices.Length - 1]++;

                // increase array indices, and roll over indexes
                for (int j = indices.Length - 1; j >= 0; j--)
                {
                    if (indices[j] >= maxIndices[j] && j > 0)
                    {
                        indices[j] = 0;
                        indices[j - 1]++;
                    }
                }
                // loop
            }

            return newArray;
        }

        // Deserializes an object - and only writes type information if necessary.
        private object DeserializeElement(Type elementType)
        {
            if (elementType.IsPrimitive)
            {
                return DeserializeValueType(elementType);
            }
            
            if ((elementType.IsClass && !elementType.IsSealed) || elementType.IsAbstract || elementType.IsInterface)
            {
                // Any System.Object, or derivable type..
                return Deserialize();
            }

            return Deserialize(elementType);
        }

        // Deserializes IList
        private object DeserializeList(Type objectType, int cacheID, ObjectMetaData metaData)
        {           
            // implements IList
            int count = ReadInt32();
            IList ilist = Activator.CreateInstance(objectType) as IList;
            if (cacheID > 0)
            {
                Cache.SetCachedObjectId(ilist, cacheID);
            }

            Type baseType = typeof(object);
            if (metaData.GenericParameters != null && metaData.GenericParameters.Length > 0)
            {
                baseType = metaData.GenericParameters[0];
            }

            for (int i = 0; i < count; i++)
            {
                object deserializedObject = DeserializeElement(baseType);
                ilist.Add(deserializedObject);
            }
            return ilist;
        }

        // Deserializes IDictionary
        private object DeserializeDictionary(Type objectType, int cacheID)
        {
            Type[] genericParameters = objectType.GetGenericArguments();

            int count = (int)Deserialize(typeof(Int32));
            IDictionary idict = Activator.CreateInstance(objectType) as IDictionary;
            if (cacheID > 0) Cache.SetCachedObjectId(idict, cacheID);

            Type keyType = typeof(object);
            Type valueType = typeof(object);
            if (genericParameters.Length > 0)
            {
                keyType = genericParameters[0];
                valueType = genericParameters[1];
            }

            for (int i = 0; i < count; i++)
            {
                object desKey = DeserializeElement(keyType);
                object value = DeserializeElement(valueType);
                idict.Add(desKey, value);
            }
            return idict;
        }

        // Deserializes classes.
        private object DeserializeComplexType(Type objectType, int cacheID)
        {         
            // Complex object
            ObjectMetaData metaData = GetMetaData(objectType);
                        
            if (metaData.DynamicSerializer != null)
            {
                // use compiled deserializer
                object newObject = metaData.DynamicSerializer.Deserialize(this, cacheID);
                return newObject;
            }

            if (metaData.IsIAltSerializable)
            {
                // Read complex object...
                object ialtobj = Activator.CreateInstance(objectType);
                if (cacheID > 0) Cache.SetCachedObjectId(ialtobj, cacheID);
                // Call this object as an IAltSerializable and move on
                ((IAltSerializable)ialtobj).Deserialize(this);
                return ialtobj;
            }

            if (metaData.ImplementsIList)
            {
                // Implements IList, use special definition for this method
                return DeserializeList(objectType, cacheID, metaData);
            }
            else if (metaData.ImplementsIDictionary)
            {
                // Implements IDictionary; use special definition for this method ..
                object dict = DeserializeDictionary(objectType, cacheID);

                // Reflect on each field, deserializing the data
                foreach (ReflectedMemberInfo info in metaData.Values)
                {
                    if (SerializeProperties == false && info.FieldType.IsSerializable == false)
                    {
                        // When deserializing fields, skip the 'nonserializable' ones.
                        continue;
                    }

                    object deserializedObject = DeserializeElement(info.FieldType);
                    info.SetValue(dict, deserializedObject);
                }
                return dict;
            }

            if (metaData.IsISerializable)
            {
                // Implements ISerializabe; use that interface for serialization/deserialization
                Dictionary<string, object> sinfo;

                SerializationInfo info = new SerializationInfo(objectType, new AltFormatter());
                StreamingContext context = new StreamingContext(StreamingContextStates.All);
                sinfo = (Dictionary<string, object>)Deserialize(typeof(Dictionary<string, object>));
                foreach (KeyValuePair<string, object> kvp in sinfo)
                {
                    info.AddValue(kvp.Key, kvp.Value);
                }

                ConstructorInfo construct = (ConstructorInfo)metaData.Extra;
                return construct.Invoke(new object[] { info, context });
            }

            if (metaData.IsGenericList)
            {
                FieldInfo itemsField = (FieldInfo)metaData.Extra;
                Type arrayType = itemsField.FieldType;
                object arrayObject = DeserializeArray(arrayType, 0);
                //object newList = Activator.CreateInstance(objectType, new object[] { ((Array)arrayObject).Length });
                object newList = Activator.CreateInstance(objectType, new object[] { arrayObject });
                if (cacheID > 0) Cache.SetCachedObjectId(newList, cacheID);
                return newList;
            }

            object obj = Activator.CreateInstance(objectType);

            // This object might be referenced by other objects
            // that it loads, so we need to cache it first!
            if (cacheID > 0)
            {
                Cache.SetCachedObjectId(obj, cacheID);
            }


            if (SerializePropertyNames)
            {
                // Deserialize each stored property
                int propertyCount = (int)((short)Deserialize(typeof(short)));
                for (int i = 0; i < propertyCount; i++)
                {
                    string propertyName = ReadString();                    
                    ReflectedMemberInfo info = metaData.FindMemberInfoByName(propertyName);
                    if (info == null)
                    {
                        // Ignore the field not found.
                        //throw new AltSerializeException("Unable to find the property '" + propertyName + "' in object type '" + objectType.FullName + "'.");
                    }
                    else
                    {
                        // Deserialize the object into the property
                        object desobj = DeserializeElement(info.FieldType);
                        info.SetValue(obj, desobj);
                    }
                }
            }
            else
            {
                // Reflect on each field, deserializing the data
                foreach (ReflectedMemberInfo info in metaData.Values)
                {
                    if (SerializeProperties == false && info.FieldType.IsSerializable == false)
                    {
                        // When deserializing fields, skip the 'nonserializable' ones.
                        continue;
                    }

                    object deserializedObject = DeserializeElement(info.FieldType);
                    info.SetValue(obj, deserializedObject);
                }
            }

            return obj;
        }

        /// <summary>
        /// Deserialize an object.  This method requires the SerializeObjectType property
        /// to be true.
        /// </summary>
        /// <returns>Returns the deserialized object of type <paramref name="objectType"/>.</returns>
        public object Deserialize()
        {
            return Deserialize(null);
        }

        /// <summary>
        /// Deserialize an object.
        /// </summary>
        /// <param name="bytes">Stream to deserialize from.</param>
        /// <param name="objectType">Type of object to deserialize to.</param>
        /// <returns>Returns the deserialized object of type <paramref name="objectType"/>.</returns>
        public object Deserialize(Type objectType)
        {
            SerializedObjectFlags serializationFlags = SerializedObjectFlags.Invalid;
            int cacheID = 0;
            serializationFlags = ReadSerializationFlags();
            if (serializationFlags == SerializedObjectFlags.IsNull)
            {
                // Null object was encoded
                return null;
            }
            if ((serializationFlags & SerializedObjectFlags.CachedItem) != 0)
            {
                // Item refers to a cached item
                cacheID = ReadInt32();
                object returnObject = Cache.GetCachedObject(cacheID);
                if (returnObject == null)
                {
                    throw new AltSerializeException("Deserialize error: A cached object was not present in the stream (" + cacheID + ")");
                }
                return returnObject;
            }
            if ((serializationFlags & SerializedObjectFlags.SetCache) != 0)
            {
                cacheID = ReadInt32();
            }
            if ((serializationFlags & SerializedObjectFlags.SystemType) != 0)
            {
                // Special flags for primitive types.
                int primitiveTypeId = Stream.ReadByte();
                if (_hashIntType.TryGetValue(primitiveTypeId, out objectType) == false)
                {
                    throw new AltSerializeException("Unknown data type encountered in stream.");
                }
            }
            if ((serializationFlags & SerializedObjectFlags.Type) != 0)
            {
                // The object type was serialized
                objectType = (Type)Deserialize(typeof(Type));
            }

            if (objectType.IsValueType)
            {
                return DeserializeValueType(objectType);
            }   
            else
            {
                object returnObject = null;

                if (objectType.IsArray)
                {                    
                    returnObject = DeserializeArray(objectType, cacheID);
                    // Reset the cache ID because DeserializeArray caches object already.
                    cacheID = 0;
                }                
                else if (objectType == typeof(String))
                {
                    returnObject = ReadString();
                }
                else if (objectType == typeof(Type))
                {
                    returnObject = ReadType();
                }
                else if (objectType == typeof(System.Globalization.CultureInfo))
                {
                    returnObject = ReadCultureInfo();
                }
                else
                {
                    returnObject = DeserializeComplexType(objectType, cacheID);
                    // Reset the cache ID because DeserializeComplexType caches object already.
                    cacheID = 0;
                }

                // Set the cached object's id.
                if (cacheID > 0)
                {
                    Cache.SetCachedObjectId(returnObject, cacheID);
                }

                return returnObject;
            }
        }

        #endregion

    }
}
