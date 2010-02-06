using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    public abstract class DynamicSerializer
    {
        public abstract void Serialize(object obj, AltSerializer serializer);

        public abstract object Deserialize(AltSerializer serializer, int cacheID);
    }
}
