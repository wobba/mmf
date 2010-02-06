using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace AltSerialize
{
    /// <summary>
    /// Data class containing information for serializing
    /// fields of a class.
    /// </summary>
    internal class ObjectField
    {
        /// <summary>
        /// Gets the Type of the field.
        /// </summary>
        public Type FieldType
        {
            get { return FieldInfo.FieldType; }
        }

        private FieldInfo _fieldInfo;
        /// <summary>
        /// Gets or sets the FieldInfo structure used
        /// to represent this type.
        /// </summary>
        public FieldInfo FieldInfo
        {
            get { return _fieldInfo; }
            set { _fieldInfo = value; }
        }        
    }
}
