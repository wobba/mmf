using System;

namespace mAdcOW.Serializer
{
    public class SerializerException : Exception
    {
        public SerializerException(string message)
            : base(message)
        {
        }
    }
}