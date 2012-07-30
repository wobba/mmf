using System;

namespace mAdcOW.Serializer
{
    class DataHelper
    {
        internal static void AssignEmptyData<T>(ref T classInstance)
        {
            try
            {
                if (typeof(T).IsValueType)
                {
                    var val = (T)Convert.ChangeType(1, typeof(T));
                    classInstance = val;
                    return;
                }
            }
            catch (InvalidCastException)
            {
            }

            var properties = classInstance.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite) continue;
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(classInstance, "mAdcOW", null);
                }
                else if (property.PropertyType.IsValueType)
                {
                    var val = Convert.ChangeType(1, property.PropertyType);
                    property.SetValue(classInstance, val, null);
                }
            }
        }
    }
}
