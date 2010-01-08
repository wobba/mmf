using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace AltSerialize
{
    /// <summary>
    /// Provides dynamic methods for serialization/deserialization.
    /// </summary>
    internal static class DynamicSerializerFactory
    {
        // Types that can be written directly to the serialization stream
        private static Type[] streamTypes =
            {
                typeof(int), typeof(uint),
                typeof(short), typeof(ushort),
                typeof(long), typeof(ulong),
                typeof(DateTime), typeof(TimeSpan),
                typeof(float), typeof(double),
                typeof(Decimal),
                typeof(Guid),
                typeof(string)
            };
        
        // Serialization methods corresponding to the stream types.
        private static MethodInfo[] _serializeMethods;
        public static MethodInfo[] SerializeMethods
        {
            get
            {
                if (_serializeMethods == null)
                {
                    Methods();
                }
                return _serializeMethods;
            }
        }

        private static MethodInfo[] _deserializeMethods;
        public static MethodInfo[] DeserializeMethods
        {
            get
            {
                if (_deserializeMethods == null)
                {
                    Methods();
                }
                return _deserializeMethods;
            }
        }

        private static MethodInfo GetDeserializeMethod(Type type)
        {
            for (int i = 0; i < streamTypes.Length; i++)
            {
                if (type == streamTypes[i])
                {
                    return DeserializeMethods[i];
                }
            }
            return null;
        }

        private static MethodInfo GetSerializerMethod(Type type)
        {
            for (int i = 0; i < streamTypes.Length; i++)
            {
                if (type == streamTypes[i])
                {
                    return SerializeMethods[i];
                }
            }
            return null;
        }

        private static void Methods()
        {
            Type serializerClassType = typeof(AltSerializer);
            _serializeMethods = new MethodInfo[streamTypes.Length];
            _deserializeMethods = new MethodInfo[streamTypes.Length];

            for (int i = 0; i < streamTypes.Length; i++)
            {
                _serializeMethods[i] = serializerClassType.GetMethod("Write", new Type[] { streamTypes[i] });
                if (_serializeMethods[i] == null)
                {
                    throw new Exception("No write method for type '" + streamTypes[i].Name + "'.");
                }

                _deserializeMethods[i] = serializerClassType.GetMethod("Read" + streamTypes[i].Name);
                if (_deserializeMethods[i] == null)
                {
                    throw new Exception("No read method for type '" + streamTypes[i].Name + "'");
                }
            }
        }

        /// <summary>
        /// Generates a dynamic serializer for the specified object type.
        /// </summary>
        public static DynamicSerializer GenerateSerializer(Type objectType)
        {
            AppDomain domain = Thread.GetDomain();
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "DynamicSerializer";
            assemblyName.Version = new Version(1, 0, 0, 0);

            AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicSerializerModule");
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

            string className = "ser_" + objectType.Name;
            // Gather up the proxy information and create a new type builder.  One that
            // inherits from Object and implements the interface passed in
            TypeBuilder typeBuilder = moduleBuilder.DefineType(className, typeAttributes, typeof(DynamicSerializer));

            Type[] serializeMethodParams = new Type[] { typeof(object), typeof(AltSerializer) };
            Type[] deserializeMethodParams = new Type[] { typeof(AltSerializer), typeof(Int32) };

            MethodBuilder serializeMethod = typeBuilder.DefineMethod("Serialize", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, null, serializeMethodParams);
            GenerateSerializeMethod(serializeMethod.GetILGenerator(), objectType);

            MethodBuilder deserializeMethod = typeBuilder.DefineMethod("Deserialize", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.HasThis, typeof(object), deserializeMethodParams);
            GenerateDeserializeMethod(deserializeMethod.GetILGenerator(), objectType);

            Type dynamicType = typeBuilder.CreateType();
            return (DynamicSerializer)Activator.CreateInstance(dynamicType);
        }

        // Generates the IL to do the serialization.
        private static void GenerateSerializeMethod(ILGenerator methodIL, Type objectType)
        {
            methodIL.DeclareLocal(objectType);

            // Cast passed object to the correct type
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Castclass, objectType);
            methodIL.Emit(OpCodes.Stloc_0);

            PropertyInfo [] properties = objectType.GetProperties();
            foreach (PropertyInfo propInfo in properties)
            {
                if (propInfo.GetCustomAttributes(typeof(DoNotSerializeAttribute), true).Length != 0)
                {
                    // Ignore anything with DoNotSerialize attribute
                    continue;
                }
                if (propInfo.CanRead == false || propInfo.CanWrite == false)
                {
                    // Ignore properties that are read-only or write-only
                    continue;
                }

                MethodInfo methodGetTypeHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo methodSerialize = typeof(AltSerializer).GetMethod("Serialize", new Type[] { typeof(object), typeof(Type) });
                MethodInfo getMethod = propInfo.GetGetMethod();

                MethodInfo quickSerializeMethod = GetSerializerMethod(propInfo.PropertyType);
                if (quickSerializeMethod != null)
                {
                    // Load serializer to stack
                    methodIL.Emit(OpCodes.Ldarg_2);
                    // Load property to stack
                    methodIL.Emit(OpCodes.Ldloc_0);
                    methodIL.Emit(OpCodes.Callvirt, getMethod);
                    // Call write method
                    methodIL.Emit(OpCodes.Callvirt, quickSerializeMethod);
                }
                else if (propInfo.PropertyType.IsValueType)
                {
                    // Load serializer and object
                    methodIL.Emit(OpCodes.Ldarg_2);
                    methodIL.Emit(OpCodes.Ldloc_0);
                    // Call the 'get' method
                    methodIL.Emit(OpCodes.Callvirt, getMethod);
                    // Box value type
                    methodIL.Emit(OpCodes.Box, propInfo.PropertyType);
                    // Push the typeof() to the stack
                    methodIL.Emit(OpCodes.Ldtoken, propInfo.PropertyType);
                    methodIL.Emit(OpCodes.Call, methodGetTypeHandle);
                    // Call the serializer
                    methodIL.Emit(OpCodes.Callvirt, methodSerialize);
                }
                else
                {
                    // Load serializer and object
                    methodIL.Emit(OpCodes.Ldarg_2);
                    methodIL.Emit(OpCodes.Ldloc_0);
                    // Call the 'get' method
                    methodIL.Emit(OpCodes.Callvirt, getMethod);
                    // Push the typeof() to the stack
                    methodIL.Emit(OpCodes.Ldtoken, propInfo.PropertyType);
                    methodIL.Emit(OpCodes.Call, methodGetTypeHandle);
                    // Call the serializer
                    methodIL.Emit(OpCodes.Callvirt, methodSerialize);
                }
            }
            
            // Return from the method
            methodIL.Emit(OpCodes.Ret);
        }

        // Generates the IL to do the deserialization.
        private static void GenerateDeserializeMethod(ILGenerator methodIL, Type objectType)
        {
            ConstructorInfo ci = objectType.GetConstructor(new Type[] { });

            LocalBuilder local0 = methodIL.DeclareLocal(objectType);
            LocalBuilder local1 = methodIL.DeclareLocal(typeof(object));

            // Create new object instance
            methodIL.Emit(OpCodes.Nop);
            methodIL.Emit(OpCodes.Newobj, ci);
            methodIL.Emit(OpCodes.Stloc_0);

            // Store cache ID, if necessary ..
            Label dontCacheLabel = methodIL.DefineLabel();
            methodIL.Emit(OpCodes.Ldarg_2);
            methodIL.Emit(OpCodes.Ldc_I4, 0);
            methodIL.Emit(OpCodes.Ceq);
            methodIL.Emit(OpCodes.Brtrue_S, dontCacheLabel);

            // Cache value
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldloc_0);
            methodIL.Emit(OpCodes.Ldarg_2);
            MethodInfo cacheMethod = typeof(AltSerializer).GetMethod("SetCachedObjectID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            methodIL.Emit(OpCodes.Callvirt, cacheMethod);

            methodIL.MarkLabel(dontCacheLabel);

            PropertyInfo[] properties = objectType.GetProperties();
            foreach (PropertyInfo propInfo in properties)
            {
                if (propInfo.GetCustomAttributes(typeof(DoNotSerializeAttribute), true).Length != 0)
                {
                    // Ignore anything with DoNotSerialize attribute
                    continue;
                }
                if (propInfo.CanRead == false || propInfo.CanWrite == false)
                {
                    // Ignore properties that are read-only or write-only
                    continue;
                }

                MethodInfo methodGetTypeHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo methodDeserialize = typeof(AltSerializer).GetMethod("Deserialize", new Type[] { typeof(Type) });
                MethodInfo setMethod = propInfo.GetSetMethod();
                MethodInfo quickDeserializeMethod = GetDeserializeMethod(propInfo.PropertyType);

                if (quickDeserializeMethod != null)
                {
                    // Load serializer
                    methodIL.Emit(OpCodes.Ldloc_0);
                    methodIL.Emit(OpCodes.Ldarg_1);
                    // Call read method
                    methodIL.Emit(OpCodes.Callvirt, quickDeserializeMethod);
                    // Set the property
                    methodIL.Emit(OpCodes.Callvirt, setMethod);
                }
                else if (propInfo.PropertyType.IsValueType)
                {
                    // Load object and serializer
                    methodIL.Emit(OpCodes.Ldloc_0);
                    methodIL.Emit(OpCodes.Ldarg_1);
                    // get typeof() for property type
                    methodIL.Emit(OpCodes.Ldtoken, propInfo.PropertyType);
                    methodIL.Emit(OpCodes.Call, methodGetTypeHandle);
                    // Call deserializer
                    methodIL.Emit(OpCodes.Callvirt, methodDeserialize);
                    // unbox value type
                    methodIL.Emit(OpCodes.Unbox_Any, propInfo.PropertyType);
                    // call the set method
                    methodIL.Emit(OpCodes.Callvirt, setMethod);
                }
                else
                {
                    // Load object and serializer
                    methodIL.Emit(OpCodes.Ldloc_0);
                    methodIL.Emit(OpCodes.Ldarg_1);
                    // get typeof() for property type
                    methodIL.Emit(OpCodes.Ldtoken, propInfo.PropertyType);
                    methodIL.Emit(OpCodes.Call, methodGetTypeHandle);
                    // call deserializer
                    methodIL.Emit(OpCodes.Callvirt, methodDeserialize);
                    // cast object to correct type
                    methodIL.Emit(OpCodes.Castclass, propInfo.PropertyType);
                    // call the set method
                    methodIL.Emit(OpCodes.Callvirt, setMethod);
                }
            }

            // Return stored object
            methodIL.Emit(OpCodes.Ldloc_0);
            methodIL.Emit(OpCodes.Stloc_1);
            Label next = methodIL.DefineLabel();
            methodIL.Emit(OpCodes.Br_S, next);
            methodIL.MarkLabel(next);
            methodIL.Emit(OpCodes.Ldloc_1);
            methodIL.Emit(OpCodes.Ret);
        }
    }
}
