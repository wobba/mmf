using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    /// <summary>
    /// If this attribute is specified on a class, then the serializer
    /// uses dynamic generated code to perform serialization/deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CompiledSerializerAttribute : Attribute
    {

    }
}
