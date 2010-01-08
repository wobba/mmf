using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AltSerialize
{
    /// <summary>
    /// When this interface is implemented, the serializer skips its internal
    /// object decomposition code and calls Serialize/Deserialize instead.
    /// </summary>
    public interface IAltSerializable
    {
        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <param name="serializer">AltSerializer to serialize object into.</param>
        void Serialize(AltSerializer serializer);

        /// <summary>
        /// Deserializes the object.
        /// </summary>
        /// <param name="deserializer">AltSerializer to deserialize the object from.</param>
        void Deserialize(AltSerializer deserializer);
    }
}
