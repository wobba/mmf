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
    public partial class AltSerializer : IDisposable
    {
        #region Internal Static Methods

        private static Dictionary<Type, int> _hashTypeInt = new Dictionary<Type, int>();
        private static Dictionary<int, Type> _hashIntType = new Dictionary<int, Type>();
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        private static void AddType(Type objectType, int hashId)
        {
            _hashIntType[hashId] = objectType;
            _hashTypeInt[objectType] = hashId;
        }

        // Adds default types to the serializer.
        private static void AddTypes()
        {
            // Primitive integer types
            AddType(typeof(int), 0);
            AddType(typeof(uint), 1);
            AddType(typeof(short), 2);
            AddType(typeof(ushort), 3);
            AddType(typeof(byte), 4);
            AddType(typeof(sbyte), 5);
            AddType(typeof(long), 6);
            AddType(typeof(ulong), 7);

            // Primitive decimal types
            AddType(typeof(float), 8);
            AddType(typeof(double), 9);
            AddType(typeof(decimal), 10);

            // Nullable integer types
            AddType(typeof(int?), 20);
            AddType(typeof(uint?), 21);
            AddType(typeof(short?), 22);
            AddType(typeof(ushort?), 23);
            AddType(typeof(byte?), 24);
            AddType(typeof(sbyte?), 25);
            AddType(typeof(long?), 26);
            AddType(typeof(ulong?), 27);

            // Nullable decimal types
            AddType(typeof(float?), 28);
            AddType(typeof(double?), 29);
            AddType(typeof(decimal?), 30);

            // Char
            AddType(typeof(char), 31);
            AddType(typeof(char?), 32);
            
            // Bool
            AddType(typeof(bool), 33);
            AddType(typeof(bool?), 34);

            // Integral array types
            AddType(typeof(int[]), 40);
            AddType(typeof(uint[]), 41);
            AddType(typeof(short[]), 42);
            AddType(typeof(ushort[]), 43);
            AddType(typeof(byte[]), 44);
            AddType(typeof(sbyte[]), 45);
            AddType(typeof(long[]), 46);
            AddType(typeof(ulong[]), 47);

            // Decimal array types
            AddType(typeof(float[]), 48);
            AddType(typeof(double[]), 49);
            AddType(typeof(decimal[]), 50);

            // Char array, bool
            AddType(typeof(char[]), 51);
            AddType(typeof(bool[]), 52);

            // Other system value types
            AddType(typeof(TimeSpan), 100);
            AddType(typeof(DateTime), 101);
            AddType(typeof(Guid), 102);
            AddType(typeof(TimeSpan?), 103);
            AddType(typeof(DateTime?), 104);
            AddType(typeof(Guid?), 105);
            AddType(typeof(string), 106);

            AddType(typeof(DateTime[]), 110);
            AddType(typeof(TimeSpan[]), 111);
            AddType(typeof(Guid[]), 112);
            AddType(typeof(string[]), 113);

            AddType(typeof(object), 250);
            AddType(typeof(object[]), 251);
            AddType(typeof(Type), 252);
            AddType(typeof(Type[]), 253);            
        }

        private static Dictionary<Type, ObjectMetaData> _metaDataHash = new Dictionary<Type, ObjectMetaData>();
        /// <summary>
        /// Gets the Hash of object Meta Data to Type dictionary.
        /// </summary>
        internal static Dictionary<Type, ObjectMetaData> MetaDataHash
        {
            get
            {
                return _metaDataHash;
            }
        }

        private static void GetAllFieldsOfType(Type type, List<FieldInfo> fieldList)
        {
            if (type == null || type == typeof(object) || type == typeof(ValueType))
            {
                return;
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                fieldList.Add(fields[i]);
            }

            GetAllFieldsOfType(type.BaseType, fieldList);
        }

        // Insertion sort
        internal static void InsertSortedMetaData(List<ReflectedMemberInfo> list, ReflectedMemberInfo minfo)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name.CompareTo(minfo.Name) > 0)
                {
                    list.Insert(i, minfo);
                    return;
                }
            }
            list.Add(minfo);
        }

        static AltSerializer()
        {
            AddTypes();
        }

        /// <summary>
        /// Gets an array of bytes from any primitive object type.
        /// </summary>
        /// <param name="obj">Object to retrieve bytes from</param>
        /// <param name="objectType">The object type passed in <paramref name="obj"/>.</param>
        /// <returns>Returns an array of bytes.</returns>
        internal static byte[] GetBytes(object obj, Type objectType)
        {
            if (objectType == typeof(int)) return BitConverter.GetBytes((int)obj);
            if (objectType == typeof(bool)) return new byte[] { (bool)obj == true ? (byte)1 : (byte)0 };
            if (objectType == typeof(byte)) return new byte[] { (byte)obj };
            if (objectType == typeof(sbyte)) return new byte[] { (byte)((sbyte)obj) };
            if (objectType == typeof(short)) return BitConverter.GetBytes((short)obj);
            if (objectType == typeof(ushort)) return BitConverter.GetBytes((ushort)obj);
            if (objectType == typeof(uint)) return BitConverter.GetBytes((uint)obj);
            if (objectType == typeof(long)) return BitConverter.GetBytes((long)obj);
            if (objectType == typeof(ulong)) return BitConverter.GetBytes((ulong)obj);
            if (objectType == typeof(float)) return BitConverter.GetBytes((float)obj);
            if (objectType == typeof(double)) return BitConverter.GetBytes((double)obj);
            if (objectType == typeof(char)) return BitConverter.GetBytes((char)obj);

            if (objectType == typeof(IntPtr)) throw new AltSerializeException("IntPtr type is not supported.");
            if (objectType == typeof(UIntPtr)) throw new AltSerializeException("UIntPtr type is not supported.");

            throw new AltSerializeException("Could not retrieve bytes from the object type " + objectType.FullName + ".");
        }

        /// <summary>
        /// Turns an array of bytes into the specified object type.
        /// </summary>
        /// <param name="bytes">Array of bytes containing the object data.</param>
        /// <param name="objectType">The type of object to convert byte array to.</param>
        /// <returns>Returns an object of the type specified in <paramref name="objectType"/>.</returns>
        internal static object ReadBytes(byte[] bytes, Type objectType)
        {
            if (objectType == typeof(bool)) return bytes[0] == (byte)1 ? true : false;
            if (objectType == typeof(byte)) return bytes[0];
            if (objectType == typeof(sbyte)) return (sbyte)bytes[0];
            if (objectType == typeof(short)) return BitConverter.ToInt16(bytes, 0);
            if (objectType == typeof(ushort)) return BitConverter.ToUInt16(bytes, 0);
            if (objectType == typeof(int)) return BitConverter.ToInt32(bytes, 0);
            if (objectType == typeof(uint)) return BitConverter.ToUInt32(bytes, 0);
            if (objectType == typeof(long)) return BitConverter.ToInt64(bytes, 0);
            if (objectType == typeof(ulong)) return BitConverter.ToUInt64(bytes, 0);
            if (objectType == typeof(float)) return BitConverter.ToSingle(bytes, 0);
            if (objectType == typeof(double)) return BitConverter.ToDouble(bytes, 0);
            if (objectType == typeof(char)) return BitConverter.ToChar(bytes, 0);

            if (objectType == typeof(IntPtr))
            {
                throw new AltSerializeException("IntPtr type is not supported.");
            }

            throw new AltSerializeException("Could not retrieve bytes from the object type " + objectType.FullName + ".");
        }

        #endregion

        #region Properties

        private bool _serializeProperties = true;
        /// <summary>
        /// If true, the fields are serialized instead of the properties.
        /// Enabled by default.
        /// </summary>
        public bool SerializeProperties
        {
            get { return _serializeProperties; }
            set { _serializeProperties = value; }
        }

        private Encoding _encoding = Encoding.Unicode;
        /// <summary>
        /// Gets or sets the string encoding to use.
        /// </summary>
        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        private Stream _stream;
        /// <summary>
        /// Gets or sets the stream used for serialization
        /// or deserialization.
        /// </summary>
        public Stream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

        private bool _cacheEnabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether or not the serializer
        /// cache is enabled.  If true, then any duplicate objects that are serialized
        /// are hashed and stored, minimizing the amount of space serialization takes.
        /// </summary>
        public bool CacheEnabled
        {
            get { return _cacheEnabled; }
            set { _cacheEnabled = value; }
        }

        private bool _serializePropertyNames;
        /// <summary>
        /// If true, the names of the properties are serialized along with
        /// the data.  This ensures that previous versions of serialized data
        /// don't misread the data stream.
        /// </summary>
        public bool SerializePropertyNames
        {
            get { return _serializePropertyNames; }
            set { _serializePropertyNames = value; }
        }

        private SerializerCache _cache = new SerializerCache();
        /// <summary>
        /// Gets the cache used by the serializer.
        /// </summary>
        internal SerializerCache Cache
        {
            get { return _cache; }
        }

        private byte[] arrayBuffer = null;
        int BlockSize = 16384;

        #endregion

        #region Private Members

        /// <summary>
        /// Gets the meta data for an object type.  If the Type doesn't exist in
        /// the meta data hash, it is created.
        /// </summary>
        /// <param name="type">The object Type to get metadata for.</param>
        /// <returns>Returns an ObjectMetaData class representing the <paramref name="type"/> parameter.</returns>
        internal ObjectMetaData GetMetaData(Type type)
        {
            if (type == null)
            {
                throw new AltSerializeException("The serializer could not get meta data for the type.");
            }

            if (MetaDataHash.ContainsKey(type))
            {
                return MetaDataHash[type];
            }

            if (type.GetCustomAttributes(typeof(CompiledSerializerAttribute), true).Length != 0)
            {
                // Compiled Serializer flag specified.
                ObjectMetaData cmetadata = new ObjectMetaData(this);
                cmetadata.ObjectType = type;
                cmetadata.DynamicSerializer = (DynamicSerializer)DynamicSerializerFactory.GenerateSerializer(type);
                MetaDataHash[type] = cmetadata;
                return cmetadata;
            }

            if (type.GetInterface(typeof(IAltSerializable).Name) != null)
            {
                // This will only be called using interface methods
                ObjectMetaData imetaData = new ObjectMetaData(this);
                imetaData.ObjectType = type;
                imetaData.IsIAltSerializable = true;
                MetaDataHash[type] = imetaData;
                return imetaData;
            }

            List<ReflectedMemberInfo> serializedProperties = new List<ReflectedMemberInfo>();

            // Get all public and non-public properties.
            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo info in props)
            {
                if (info.GetCustomAttributes(typeof(DoNotSerializeAttribute), true).Length > 0)
                {
                    // Don't add it to the list if a nonserialized attribute is set
                    continue;
                }
                if (info.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                if (info.CanRead == false || info.CanWrite == false)
                {
                    // If its a read or write only property, don't bother with it
                    continue;
                }
                //serializedFields.Add(new ReflectedMemberInfo(info));
                InsertSortedMetaData(serializedProperties, new ReflectedMemberInfo(info));
            }


            // Get all public and non-public fields.
            List<ReflectedMemberInfo> serializedFields = new List<ReflectedMemberInfo>();
            List<FieldInfo> fields = new List<FieldInfo>();
            GetAllFieldsOfType(type, fields);
            foreach (FieldInfo info in fields)
            {
                if (info.IsNotSerialized == true) continue;
                InsertSortedMetaData(serializedFields, new ReflectedMemberInfo(info));
            }

            ObjectMetaData metaData = new ObjectMetaData(this);
            metaData.ObjectType = type;
            metaData.Fields = serializedFields.ToArray();
            metaData.Properties = serializedProperties.ToArray();

            if (type.IsGenericType)
            {
                metaData.GenericTypeDefinition = type.GetGenericTypeDefinition();
                metaData.GenericParameters = type.GetGenericArguments();
            }

            if (type.GetInterface(typeof(ISerializable).Name) != null)
            {
                // Indicate that the ISerializable interface is available.
                metaData.IsISerializable = true;
                // Store constructor info for ISerializables.
                metaData.Extra = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
            }

            // Check for an IList implementation.
            Type iface = type.GetInterface("System.Collections.IList");
            if (iface != null)
            {
                metaData.ImplementsIList = true;
            }

            // Check for an IDictionary implementation.
            iface = type.GetInterface("System.Collections.IDictionary");
            if (iface != null)
            {
                metaData.ImplementsIDictionary = true;
            }

            // Check for a generic List<> implementation.
            if (metaData.GenericTypeDefinition == typeof(List<>))
            {
                metaData.IsGenericList = true;
                metaData.ImplementsIList = false;
                metaData.Extra = type.GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
                metaData.SizeField = type.GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic);
                metaData.SerializeMethod = type.GetMethod("ToArray");
            }


            // Store in hash and return the structure
            MetaDataHash[type] = metaData;
            return metaData;
        }

        /// <summary>
        /// Sets the Cache ID of an object.  Used in dynamic serialization; this method
        /// is not intended to be called from any other source.
        /// </summary>
        public void SetCachedObjectID(object obj, int cacheID)
        {
            Cache.SetCachedObjectId(obj, cacheID);
        }

        // Writes the serialization flags as a byte.
        private void WriteSerializationFlags(SerializedObjectFlags flags)
        {
            this.Stream.WriteByte((byte)flags);
        }

        // Reads a byte as serialization flags.
        private SerializedObjectFlags ReadSerializationFlags()
        {
            return (SerializedObjectFlags)this.Stream.ReadByte();
        }

        // Special method to write Type objects.
        private void WriteType(Type objectType)
        {
            int typeHash;
            if (_hashTypeInt.TryGetValue(objectType, out typeHash))
            {
                // Write a 1-byte hash for this type
                WriteUInt24(0);
                this.Stream.WriteByte((byte)typeHash);
            }
            else
            {
                // Write the full type name.
                byte[] bytes = ASCIIEncoding.ASCII.GetBytes(objectType.AssemblyQualifiedName);
                WriteUInt24((int)bytes.Length);
                this.Stream.Write(bytes, 0, bytes.Length);
            }
        }

        // Special method to read Type objects.
        private Type ReadType()
        {
            int strLen = this.ReadUInt24();
            if (strLen == 0)
            {
                // Hashed type...
                strLen = this.Stream.ReadByte();
                return _hashIntType[strLen];
            }
            else
            {
                byte[] bytes = new byte[strLen];
                this.Stream.Read(bytes, 0, strLen);
                string typeName = ASCIIEncoding.ASCII.GetString(bytes);

                Type returnType;
                if (_types.TryGetValue(typeName, out returnType))
                {
                    return returnType;
                }

                returnType = Type.GetType(typeName);
                if (returnType == null)
                {
                    throw new AltSerializeException("Unable to GetType object type '" + typeName + ".");
                }
                _types[typeName] = returnType;
                return returnType;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of an AltSerializer.
        /// </summary>
        public AltSerializer()
            : this(new MemoryStream())
        {
        }

        /// <summary>
        /// Creates a new instance of an AltSerializer.
        /// </summary>
        /// <param name="bytes">Array of bytes to deserialize from.</param>
        public AltSerializer(byte[] bytes)
            : this(new MemoryStream(bytes))
        {            
        }

        /// <summary>
        /// Creates a new instance of an AltSerializer.
        /// </summary>
        /// <param name="stream">Stream to serializer from or into.</param>
        public AltSerializer(Stream stream)
        {
            InitStaticCache();
            this.Stream = stream;
            arrayBuffer = new byte[BlockSize];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears the cache and resets the stream.
        /// </summary>
        public void Reset()
        {
            if (this.Stream != null)
            {
                this.Stream.Position = 0;
            }
            Cache.Clear();
        }

        /// <summary>
        /// Caches an object for the serializer.
        /// </summary>
        /// <remarks>The object being cached is permanately cached, and when encountered by the serializer,
        /// is always marked as a reference.</remarks>
        /// <param name="cachedObject">Object to cache.</param>
        public void CacheObject(object cachedObject)
        {
            Cache.CacheObject(cachedObject, true);
        }

        #endregion

        #region Non-Public Methods

        private void CacheType(Type objType)
        {
            GetMetaData(objType);
            CacheObject(objType);
        }

        // Creates a cache of static objects.
        private void InitStaticCache()
        {
            Type[] listTypesToCache ={
                typeof(List<int>), typeof(List<uint>), typeof(List<byte>), typeof(List<sbyte>), typeof(List<short>), typeof(List<ushort>), typeof(List<long>), typeof(List<ulong>), typeof(List<DateTime>), typeof(List<TimeSpan>), typeof(List<decimal>), typeof(List<float>), typeof(List<double>), typeof(List<Guid>)
            };

            foreach (Type type in listTypesToCache)
            {
                CacheType(type);
            }
        }

        #endregion

        #region Write Methods

        // Write 24-bit unsigned int
        private void WriteUInt24(int value)
        {
            Write((byte)(value & 0xFF));
            Write((byte)(((value >> 8) & 0xFF)));
            Write((byte)(((value >> 16) & 0xFF)));
        }

        /// <summary>
        /// Writes a byte to the serialization stream.
        /// </summary>
        public void Write(byte val)
        {
            Stream.WriteByte(val);
        }

        /// <summary>
        /// Writes a signed byte to the serialization stream.
        /// </summary>
        public void Write(sbyte val)
        {
            Stream.WriteByte((byte)val);
        }

        /// <summary>
        /// Writes an array of bytes to the serialization stream.
        /// </summary>
        /// <param name="bytes">Array of bytes to write.</param>
        /// <param name="offset">Offset to begin writing from.</param>
        /// <param name="count">Number of bytes to write.</param>
        public void Write(byte[] bytes, int offset, int count)
        {
            Stream.Write(bytes, offset, count);
        }

        /// <summary>
        /// Writes an array of bytes to the serialization stream.
        /// </summary>
        /// <param name="bytes">Array of bytes to write.</param>
        public void Write(byte[] bytes)
        {
            Stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes a signed 32-bit value to the serialization stream.
        /// </summary>
        public void Write(Int32 value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Writes an unsigned 32-bit value to the serialization stream.
        /// </summary>
        public void Write(UInt32 value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Writes a string to the serialization stream.
        /// </summary>
        public void Write(string str)
        {
            if (str == null)
            {
                // write max 24-bit to indicate null.
                WriteUInt24(0xFFFFFF);
                return;
            }

            // Write the size of the string
            int byteCount = this.Encoding.GetByteCount(str);
            WriteUInt24(byteCount);

            if (byteCount > BlockSize)
            {
                byte [] bytes = new byte[byteCount];
                this.Encoding.GetBytes(str, 0, str.Length, bytes, 0);
                this.Stream.Write(bytes, 0, byteCount);
            }
            else
            {
                this.Encoding.GetBytes(str, 0, str.Length, arrayBuffer, 0);
                this.Stream.Write(arrayBuffer, 0, byteCount);
            }
        }

        /// <summary>
        /// Writes a signed 16-bit value to the stream.
        /// </summary>
        public void Write(short value)
        {
            Stream.WriteByte((byte)(value & 0xFF));
            Stream.WriteByte((byte)((value >> 8) & 0xFF));
        }

        /// <summary>
        /// Writes an unsigned 16-bit value to the serialization stream.
        /// </summary>
        public void Write(UInt16 value)
        {
            Stream.WriteByte((byte)(value & 0xFF));
            Stream.WriteByte((byte)((value >> 8) & 0xFF));
        }

        /// <summary>
        /// Writes a signed 64-bit value to the serialization stream.
        /// </summary>
        public void Write(long value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Writes an unsigned 64-bit value to the serialization stream.
        /// </summary>
        public void Write(UInt64 value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Writes a DateTime to the serialization stream.
        /// </summary>
        public void Write(DateTime value)
        {
#if MONO
            long binary = value.ToFileTime();
#else
            long binary = value.ToBinary();
#endif
            Write(binary);
        }

        /// <summary>
        /// Writes a TimeSpan to the serialization stream.
        /// </summary>
        public void Write(TimeSpan value)
        {
            long binary = value.Ticks;
            Write(binary);
        }

        /// <summary>
        /// Writes a Guid to the serialization stream.
        /// </summary>
        public void Write(Guid value)
        {
            Write(value.ToByteArray());
        }

        /// <summary>
        /// Writes a Decimal to the serialization stream.
        /// </summary>
        public void Write(Decimal value)
        {
            int[] bits = Decimal.GetBits(value);
            Write(bits[0]);
            Write(bits[1]);
            Write(bits[2]);
            Write(bits[3]);
        }

        /// <summary>
        /// Writes a double-precision floating point value to the serialization stream.
        /// </summary>
        public void Write(double value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 8);
        }

        /// <summary>
        /// Writes a single-precision floating point value to the serialization stream.
        /// </summary>
        public void Write(float value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        /// <summary>
        /// Writes a Char to the serialization stream.
        /// </summary>
        public void Write(char value)
        {
            Stream.Write(BitConverter.GetBytes(value), 0, 2);
        }

        /// <summary>
        /// Writes a CultureInfo structure to the serialization stream.
        /// </summary>
        public void WriteCultureInfo(System.Globalization.CultureInfo info)
        {
            SerializeValueType(info.LCID, typeof(int));
        }

        #endregion

        #region Read Methods

        // Read 24-bit unsigned int
        private int ReadUInt24()
        {
            // Read first two bytes.
            int newValue;
            newValue = ReadByte();
            newValue += (ReadByte() << 8);
            newValue += (ReadByte() << 16);
            return newValue;
        }

        /// <summary>
        /// Reads a signed 32-bit integer from the serialization stream.
        /// </summary>
        public Int32 ReadInt32()
        {
            Stream.Read(arrayBuffer, 0, 4);
            return BitConverter.ToInt32(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from the serialization stream.
        /// </summary>
        public UInt32 ReadUInt32()
        {
            Stream.Read(arrayBuffer, 0, 4);
            return BitConverter.ToUInt32(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads a Byte from the serialization stream.
        /// </summary>
        public int ReadByte()
        {
            return Stream.ReadByte();
        }

        /// <summary>
        /// Reads a signed byte from the serialization stream.
        /// </summary>
        public sbyte ReadSByte()
        {
            return (sbyte)Stream.ReadByte();
        }

        /// <summary>
        /// Reads an array of bytes from the serialization stream.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Returns an array of bytes read from the deserialization stream.</returns>
        public byte[] ReadBytes(int count)
        {
            byte[] ret = new byte[count];
            ReadBytes(ret, 0, count);
            return ret;
        }

        /// <summary>
        /// Reads an array of bytes from the serialization stream.
        /// </summary>
        /// <param name="bytes">Byte array to read into.</param>
        /// <param name="offset">Starting offset of <paramref>bytes</paramref>.</param>
        /// <param name="count">Number of bytes to read.</param>
        public void ReadBytes(byte[] bytes, int offset, int count)
        {
            Stream.Read(bytes, offset, count);
        }

        /// <summary>
        /// Reads a string from the serialization stream.
        /// </summary>
        public string ReadString()
        {
            int byteCount = ReadUInt24();

            if (byteCount == 0xFFFFFF)
            {
                // null was written.
                return null;
            }

            byte[] byteArray;
            if (byteCount > BlockSize)
            {
                // For very large arrays, allocate space enough for them individually.
                byteArray = new byte[byteCount];
            }
            else
            {
                byteArray = arrayBuffer;
            }

            Stream.Read(byteArray, 0, byteCount);
            return this.Encoding.GetString(byteArray, 0, byteCount);            
        }

        /// <summary>
        /// Reads a signed 16-bit value from the serialization stream.
        /// </summary>
        public short ReadInt16()
        {
            Stream.Read(arrayBuffer, 0, 2);
            return (short)(arrayBuffer[0] + (arrayBuffer[1] << 8));
        }

        /// <summary>
        /// Reads an unsigned 16-bit value from the serialization stream.
        /// </summary>
        public UInt16 ReadUInt16()
        {
            Stream.Read(arrayBuffer, 0, 2);
            return (UInt16)(arrayBuffer[0] + (arrayBuffer[1] << 8));
        }

        /// <summary>
        /// Reads a signed 64-bit value from the serialization stream.
        /// </summary>
        public long ReadInt64()
        {
            Stream.Read(arrayBuffer, 0, 8);
            return BitConverter.ToInt64(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads an unsigned 64-bit value from the serialization stream.
        /// </summary>
        public UInt64 ReadUInt64()
        {
            Stream.Read(arrayBuffer, 0, 8);
            return BitConverter.ToUInt64(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads a DateTime from the serialization stream.
        /// </summary>
        public DateTime ReadDateTime()
        {
            long value = ReadInt64();
#if MONO
            return new DateTime(value);
#else
            return DateTime.FromBinary(value);
#endif
        }

        /// <summary>
        /// Reads a TimeSpan from the serialization stream.
        /// </summary>
        public TimeSpan ReadTimeSpan()
        {
            long value = ReadInt64();
            return new TimeSpan(value);
        }

        // Guid requires a 16-byte array.
        private byte[] guidArray = new byte[16];

        /// <summary>
        /// Reads a Guid from the serialization stream.
        /// </summary>
        public Guid ReadGuid()
        {
            Stream.Read(guidArray, 0, 16);
            return new Guid(guidArray);
        }

        /// <summary>
        /// Reads a Char from the serialization stream.
        /// </summary>
        public char ReadChar()
        {
            Stream.Read(arrayBuffer, 0, 2);
            return BitConverter.ToChar(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads a Decimal from the serialization stream.
        /// </summary>
        public decimal ReadDecimal()
        {
            int[] newDecimal = new int[4];
            newDecimal[0] = ReadInt32();
            newDecimal[1] = ReadInt32();
            newDecimal[2] = ReadInt32();
            newDecimal[3] = ReadInt32();
            return new Decimal(newDecimal);
        }

        /// <summary>
        /// Reads a double-precision floating point value from the serialization stream.
        /// </summary>
        public double ReadDouble()
        {
            Stream.Read(arrayBuffer, 0, 8);
            return BitConverter.ToDouble(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads a single-precision floating point value from the serialization stream.
        /// </summary>
        public float ReadSingle()
        {
            Stream.Read(arrayBuffer, 0, 4);
            return BitConverter.ToSingle(arrayBuffer, 0);
        }

        /// <summary>
        /// Reads culture info from the serialization stream.
        /// </summary>
        public System.Globalization.CultureInfo ReadCultureInfo()
        {
            int LCID = (int)DeserializeValueType(typeof(int));
            return System.Globalization.CultureInfo.GetCultureInfo(LCID);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes of the AltSerializer.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes of the AltSerializer.
        /// </summary>
        protected virtual void Dispose(bool disposeAll)
        {
            if (_stream != null)
            {
                _stream.Dispose();
            }
            Cache.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
