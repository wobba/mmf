using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Check if a Type is a value type
    /// </summary>
    internal class ValueTypeCheck
    {
        private readonly Type _type;

        public ValueTypeCheck(Type objectType)
        {
            _type = objectType;
        }

        internal bool OnlyValueTypes()
        {
            if (_type.IsPrimitive) return true;
            return PropertySizesAreDefined() && FieldSizesAreDefined();
        }

        private bool FieldSizesAreDefined()
        {
            foreach (
                FieldInfo fieldInfo in
                    _type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (fieldInfo.FieldType.IsPrimitive) continue;
                if (!HasMarshalDefinedSize(fieldInfo)) return false;
            }
            return true;
        }

        private bool PropertySizesAreDefined()
        {
            foreach (
                PropertyInfo propertyInfo in
                    _type.GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public |
                                        BindingFlags.NonPublic))
            {
                if (propertyInfo.CanRead == false || propertyInfo.CanWrite == false)
                {
                    return false;
                }
                if (propertyInfo.PropertyType.IsPrimitive) continue;
                if (!HasMarshalDefinedSize(propertyInfo)) return false;
            }
            return true;
        }

        private bool HasMarshalDefinedSize(MemberInfo info)
        {
            object[] customAttributes = info.GetCustomAttributes(typeof (MarshalAsAttribute), true);
            if (customAttributes.Length == 0) return false;
            MarshalAsAttribute attribute = (MarshalAsAttribute) customAttributes[0];
            return attribute.SizeConst > 0;
        }
    }
}