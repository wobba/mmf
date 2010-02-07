using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    [Flags]
    public enum SerializeFlags
    {
        /// <summary>
        /// Do not include any extra information for serialization.
        /// </summary>
        None = 0,
        /// <summary>
        /// Serialize the property names of each property.
        /// Use this flag whenever the properties of an object may change,
        /// or if for any reason the order of the reflected properties would
        /// be different.
        /// </summary>
        SerializePropertyNames = 0x1,
        /// <summary>
        /// If this flag is set, then the serializer attempts to cache objects it's
        /// seen before.  This allows the serializer to serialize objects that have
        /// circular references, and can reduce the amount of space the serialized
        /// object consumes.
        /// </summary>
        SerializationCache = 0x2,
        /// <summary>
        /// Serializes the properties of an object.  If this flag is not set,
        /// then the fields are serialized.
        /// </summary>
        SerializeProperties = 0x10,
        /// <summary>
        /// Enables all serialization flags.
        /// </summary>
        All = SerializePropertyNames + SerializationCache,
    }
}
