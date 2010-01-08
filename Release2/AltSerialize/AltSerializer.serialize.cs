using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization;

namespace AltSerialize
{
    public partial class AltSerializer
    {
        #region Serialization Methods

        // Serializes value types.
        private void SerializeValueType(object obj, Type objectType)
        {
            if (objectType.IsGenericType)
            {
                objectType = objectType.GetGenericArguments()[0];
                if (obj == null)
                {
                    WriteSerializationFlags(SerializedObjectFlags.IsNull);
                    return;
                }
                WriteSerializationFlags(SerializedObjectFlags.None);
            }
            if (objectType.IsPrimitive)
            {
                byte[] bytes = GetBytes(obj, objectType);
                this.Stream.Write(bytes, 0, bytes.Length);
            }
            else if (objectType == typeof(DateTime))
            {
#if MONO
                Serialize(((DateTime)obj).ToFileTime(), typeof(long));
#else
                Write(((DateTime)obj).ToBinary());
#endif
            }
            else if (objectType == typeof(TimeSpan))
            {
                Write(((TimeSpan)obj).Ticks);
            }
            else if (objectType == typeof(Guid))
            {
                byte[] bytes = ((Guid)obj).ToByteArray();
                this.Stream.Write(bytes, 0, 16);
            }
            else if (objectType.IsEnum)
            {
                /// <TODO>make this quicker</TODO>
                Type enumType = Enum.GetUnderlyingType(objectType);
                object realObject = Convert.ChangeType(obj, enumType);
                Serialize(realObject, enumType);
            }
            else if (objectType == typeof(Decimal))
            {
                int[] bits = Decimal.GetBits((decimal)obj);
                Write(bits[0]);
                Write(bits[1]);
                Write(bits[2]);
                Write(bits[3]);
            }
            else
            {
                // Use Marshaling to get bytes of the value type
                // TODO: mikael - check if it can be marshalled
                int size = Marshal.SizeOf(objectType);                
                byte[] bytes = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(obj, ptr, false);                
                Marshal.Copy(ptr, bytes, 0, size);
                Write(bytes, 0, bytes.Length);
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Quickly serialize an value type array.
        private void SerializeValueTypeArray(Array array, Type baseType, int count)
        {
            int size = Marshal.SizeOf(baseType);
            if (baseType == typeof(bool)) size = 1;
            else if (baseType == typeof(char)) size = 2;

            Write(count);
            // Write small chunks at a time to avoid allocations.
            int length = count * size;
            int ptr = 0;
            while (ptr < length)
            {
                int copyAmount = BlockSize;
                if (ptr + BlockSize > length)
                {
                    copyAmount = length - ptr;
                }
                Buffer.BlockCopy(array, ptr, arrayBuffer, 0, copyAmount);
                Stream.Write(arrayBuffer, 0, copyAmount);
                ptr += copyAmount;
            }
        }

        // Quickly serializes a byte array.
        private void SerializeByteArray(object array, int count)
        {
            byte[] byteArray = (byte[])array;
            Write(count);
            Write(byteArray, 0, count);
        }

        // Serializes arrays.  The count argument is only used on single-dimensional arrays.
        // If count is -1, the whole array is serialized.
        private void SerializeArray(object obj, Type objectType, int count)
        {
            Array objectArray = (Array)obj;
            if (objectType == null)
            {
                objectType = obj.GetType().GetElementType();
            }

            Write((byte)objectArray.Rank);
            if (count < 0)
            {
                count = objectArray.Length;
            }

            Type baseType = objectType;
            if (objectArray.Rank == 1 && baseType.IsPrimitive)
            {
                if (baseType == typeof(byte))
                {
                    SerializeByteArray(obj, count);
                    return;
                }
                else
                {
                    SerializeValueTypeArray((Array)obj, baseType, count);
                    return;
                }
            }

            // Write array size and type                      
            int length = 0;
            int[] maxIndices = new int[objectArray.Rank];
            if (objectArray.Rank == 1)
            {
                // Single-dimensioned array
                Write((int)count);
                length = count;
            }
            else
            {
                for (int i = 0; i < objectArray.Rank; i++)
                {
                    maxIndices[i] = objectArray.GetLength(i);
                    length += maxIndices[i];
                    Write((int)maxIndices[i]);
                }
            }

            int[] indices = new int[objectArray.Rank];
            for (int i = 0; i < count; i++)
            {
                object arrayValue = objectArray.GetValue(indices);
                SerializeElement(arrayValue, baseType);

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
        }

        // Serializes an object, storing type information only if necessary.
        private void SerializeElement(object obj, Type elementType)
        {
            if (elementType.IsPrimitive)
            {
                SerializeValueType(obj, elementType);
            }
            else if ((elementType.IsClass && !elementType.IsSealed) || elementType.IsAbstract || elementType.IsInterface)
            {
                // Any System.Object, or derivable type.. write type info
                Serialize(obj);
            }
            else
            {
                // Sealed class (non-derivable), fixed type.
                Serialize(obj, elementType);
            }
        }

        // Serializes IList
        private void SerializeList(object genericObject, Type objectType, ObjectMetaData metaData)
        {            
            IList ilist = genericObject as IList;
            if (ilist == null)
            {
                throw new AltSerializeException("The object type " + objectType.FullName + " does not implement IList.");
            }

            IEnumerator enumerator = ilist.GetEnumerator();
            Write((int)ilist.Count);
            while (enumerator.MoveNext())
            {
                SerializeElement(enumerator.Current, objectType);
            }
        }

        // Serializes IDictionary
        private void SerializeDictionary(object genericObject, Type objectType)
        {
            Type[] genericParameters = objectType.GetGenericArguments();

            IDictionary idict = genericObject as IDictionary;
            if (idict == null)
            {
                throw new AltSerializeException("The object type " + objectType.FullName + " does not implement IDictionary.");
            }

            IDictionaryEnumerator enumerator = idict.GetEnumerator();

            Type keyType = typeof(object);
            Type valueType = typeof(object);
            if (genericParameters.Length > 0)
            {
                keyType = genericParameters[0];
                valueType = genericParameters[1];
            }

            Serialize(idict.Count, typeof(Int32));
            while (enumerator.MoveNext())
            {
                SerializeElement(enumerator.Key, keyType);
                SerializeElement(enumerator.Value, valueType);
            }
        }

        // Serializes classes
        private void SerializeComplexType(object obj, Type objectType)
        {
            // Write complex object...                    
            ObjectMetaData metaData = GetMetaData(objectType);
            if (metaData.DynamicSerializer != null)
            {
                // use compiled serializer
                metaData.DynamicSerializer.Serialize(obj, this);
                return;
            }

            if (metaData.IsIAltSerializable)
            {
                // Call this object as an IAltSerializable and move on
                ((IAltSerializable)obj).Serialize(this);
                return;
            }

            // Check if the object implements any generic types.
            if (metaData.ImplementsIList)
            {
                SerializeList(obj, objectType, metaData);
                return;
            }
            else if (metaData.ImplementsIDictionary)
            {
                SerializeDictionary(obj, objectType);

                foreach (ReflectedMemberInfo info in metaData.Values)
                {
                    object objValue = info.GetValue(obj);

                    if (SerializePropertyNames == true)
                    {
                        // Write the name of the property before serializing it
                        Write(info.Name);
                    }

                    if (SerializeProperties == false && info.FieldType.IsSerializable == false)
                    {
                        // When serializing fields, skip the 'nonserializable' ones.
                        continue;
                    }

                    SerializeElement(objValue, info.FieldType);
                }
                return;
            }

            if (metaData.IsISerializable)
            {
                // Object implements ISerializabe; use that interface for serialization.
                SerializationInfo info = new SerializationInfo(objectType, new AltFormatter());
                StreamingContext context = new StreamingContext(StreamingContextStates.All);
                ((ISerializable)obj).GetObjectData(info, context);
                SerializationInfoEnumerator e = info.GetEnumerator();
                Dictionary<string, object> sinfo = new Dictionary<string, object>();
                while (e.MoveNext())
                {
                    sinfo[e.Name] = e.Value;
                }
                Serialize(sinfo, typeof(Dictionary<string, object>));
                return;
            }

            if (metaData.IsGenericList)
            {
                // Optimizations for generic list.
                FieldInfo itemsField = (FieldInfo)metaData.Extra;
                object arrayObject = itemsField.GetValue(obj);
                int count = ((ICollection)obj).Count;
                SerializeArray(arrayObject, metaData.GenericParameters[0], count);
                return;
            }

            if (SerializePropertyNames)
            {
                // Write out the number of properties we're going to serialize
                Write((short)metaData.Values.Length);
            }

            foreach (ReflectedMemberInfo info in metaData.Values)
            {
                object objValue = info.GetValue(obj);

                if (SerializePropertyNames == true)
                {
                    // Write the name of the property before serializing it
                    Write(info.Name);
                }

                if (SerializeProperties == false && info.FieldType.IsSerializable == false)
                {
                    // When serializing fields, skip the 'nonserializable' ones.
                    continue;
                }

                SerializeElement(objValue, info.FieldType);
            }
        }

        /// <summary>
        /// Serializes an object into the serializer stream.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        public void Serialize(object obj)
        {
            Serialize(obj, null);
        }

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <remarks>Using this overload and specifying an object type prevents
        /// Type information for the object from being written.  This means
        /// that the call to deserialize must also specify the object type.</remarks>
        /// <param name="obj">Object to serialize</param>
        /// <param name="objectType">Object Type to serialize.</param>
        public void Serialize(object obj, Type objectType)
        {
            if (obj == null)
            {
                WriteSerializationFlags(SerializedObjectFlags.IsNull);
                return;
            }

            SerializedObjectFlags objectFlags = SerializedObjectFlags.None;
            int cacheID = 0;

            bool isCacheable = true;
            if (objectType == null)
            {
                // Object type was not explicitly given.
                objectType = obj.GetType();
                if (obj.GetType().BaseType == typeof(Type))
                {
                    // Type information - specialized code to handle this
                    objectFlags = SerializedObjectFlags.SystemType;
                    objectType = typeof(Type);
                }
                else if (objectType == typeof(string))
                {
                    // Strings are "system types", but are cached.
                    objectFlags = SerializedObjectFlags.SystemType;
                    isCacheable = true;
                }
                else if (_hashTypeInt.ContainsKey(objectType))
                {
                    // System type - just record type, no caching
                    objectFlags = SerializedObjectFlags.SystemType;
                    isCacheable = false;
                }
                else
                {
                    // Need to write out type.
                    objectFlags = SerializedObjectFlags.Type;
                }
            }
            
            // Handle cacheing, if enabled - don't cache system types, value types, or enums
            if (CacheEnabled && isCacheable && !objectType.IsEnum && !objectType.IsValueType)
            {
                objectFlags |= SerializedObjectFlags.SetCache;
                cacheID = Cache.GetObjectCacheID(obj, objectType);
                if (cacheID != 0)
                {
                    // Write the cache ID and return
                    WriteSerializationFlags(SerializedObjectFlags.CachedItem);
                    Write((Int32)cacheID);
                    return;
                }

                // Get a cache ID for this object
                cacheID = Cache.CacheObject(obj, false);
            }


            // Write leading byte, and type information
            WriteSerializationFlags(objectFlags);
            if ((objectFlags & SerializedObjectFlags.SetCache) != 0)
            {
                Write((Int32)cacheID);
            }   
            if ((objectFlags & SerializedObjectFlags.SystemType) != 0)
            {
                Write((byte)_hashTypeInt[objectType]);
            }
            if ((objectFlags & SerializedObjectFlags.Type) != 0)
            {
                Serialize(objectType, typeof(Type));                
            }

            // Serialize the object
            if (objectType.IsValueType)
            {
                SerializeValueType(obj, objectType);
            }
            else
            {
                if (objectType.IsArray)
                {
                    SerializeArray(obj, null, -1);
                }
                else if (objectType == typeof(String))
                {
                    Write((string)obj);
                }
                else if (objectType == typeof(Type))
                {
                    WriteType(obj as Type);
                }
                else if (objectType == typeof(System.Globalization.CultureInfo))
                {
                    WriteCultureInfo(obj as System.Globalization.CultureInfo);
                }
                else
                {
                    SerializeComplexType(obj, objectType);
                }
            }
        }

        #endregion
    }
}