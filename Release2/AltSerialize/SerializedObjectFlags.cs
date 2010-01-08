using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    /// <summary>
    /// Flags indicating how the data was serialized.
    /// </summary>
    [Flags]
    internal enum SerializedObjectFlags : byte
    {
        None = 0x0,
        IsNull = 0x1,
        SetCache = 0x2,
        CachedItem = 0x4,
        PropertyName = 0x8,
        FieldName = 0x10,
        Type = 0x20,
        SystemType = 0x40,
        Array = 0x80,
        Invalid = 0xFF
    }
}
