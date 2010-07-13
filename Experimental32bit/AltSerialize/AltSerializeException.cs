using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    /// <summary>
    /// Thrown when the AltSerializer encounters an error.
    /// </summary>
    [Serializable]
    public class AltSerializeException : Exception
    {
        public AltSerializeException(string message)
            : base(message)
        {

        }

        public AltSerializeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AltSerializeException()
            : base()
        {
        }
    }
}
