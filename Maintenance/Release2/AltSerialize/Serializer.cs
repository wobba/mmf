using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace AltSerialize
{    
    /// <summary>
    /// Static methods for easy, thread-safe serialization.
    /// </summary>
    public static class Serializer
    {        
        #region Properties

        private static ByteSerializer _serializer = new ByteSerializer();

        /// <summary>
        /// Gets or sets the default serialization flags.
        /// This method defaults to None.
        /// </summary>
        public static SerializeFlags DefaultSerializeFlags
        {
            get { return _serializer.DefaultSerializeFlags; }
            set { _serializer.DefaultSerializeFlags = value; }
        }

        /// <summary>
        /// Gets the default string encoding to use for serialization.
        /// </summary>
        public static Encoding DefaultEncoding
        {
            get { return _serializer.DefaultEncoding; }
            set { _serializer.DefaultEncoding = value; }
        }        
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Serializes an object using the default serialization flags
        /// and returns a byte array of the result.
        /// </summary>
        /// <param name="anObject">The object to serialize.</param>
        /// <returns>Returns a byte array of the serialized object.</returns>
        public static byte[] Serialize(object anObject)
        {
            return _serializer.Serialize(anObject);
        }

        /// <summary>
        /// Serializes an object and returns a byte array of the result.
        /// </summary>
        /// <param name="anObject">The object to serialize.</param>
        /// <param name="flags">Flags to control the serialization.</param>
        /// <returns>Returns a byte array of the serialized object.</returns>
        public static byte[] Serialize(object anObject, Type objectType)
        {
            return _serializer.Serialize(anObject, objectType);
        }

        /// <summary>
        /// Deserializes an object using the default serialization flags
        /// and returns the result.
        /// </summary>
        /// <param name="bytes">Array of bytes containing the serialized object.</param>
        /// <param name="objectType">The object type contained in the serialized byte array.</param>
        /// /// <param name="flags">Flags to control the deserialization.</param>
        /// <returns>Returns the deserialized object.</returns>
        public static object Deserialize(byte[] bytes, Type objectType)
        {
            return _serializer.Deserialize(bytes, objectType);
        }

        /// <summary>
        /// Deserializes an object.
        /// </summary>
        /// <param name="bytes">Array of bytes containing the serialized object.</param>
        /// <returns>Returns the deserialized object.</returns>
        public static object Deserialize(byte[] bytes)
        {
            return _serializer.Deserialize(bytes);
        }

        /// <summary>
        /// Adds an object to the serialization cache.
        /// </summary>
        /// <remarks>This method makes a permanant addition to the serialization class.
        /// Any time the serializer encounters the object, it will use the cached reference
        /// instead of serializing the entire object.</remarks>
        /// <param name="cachedObject">Object to cache.</param>
        public static void CacheObject(object cachedObject)
        {
            _serializer.CacheObject(cachedObject);
        }

        #endregion
    }
}