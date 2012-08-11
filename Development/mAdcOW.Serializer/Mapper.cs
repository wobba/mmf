using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using EmitMapper;
using EmitMapper.Mappers;
using EmitMapper.MappingConfiguration;
using ProtoBuf;

namespace mAdcOW.Serializer
{
    internal class SystemTypes
    {
        //Can be static overall..
        internal static HashSet<string> fclDlls = new HashSet<string>
        {
            "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        };  

    }

    //TODO: Nested User Defined Types
    public class Mapper<T>
    {
        private Assembly _assembly;
        private AssemblyBuilder _assemblyBuilder;
        private bool _assemblyLoaded;
        private string _assemblyName = string.Empty;
        private Type _mappedType;
        private AssemblyName _name;
        private ObjectsMapperBaseImpl _objectsMapperFrom;
        private ObjectsMapperBaseImpl _objectsMapperTo;
        private Type _type;

        private const MethodAttributes GetSetAttr =
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

       

        #region Public Properties

        /// <summary>
        /// Gets the appropriate cloned type for T, after marking the
        /// public properties with the right attributes
        /// </summary>
        /// <returns></returns>
        public Type GetMappedType()
        {
            //EmitMapper can create only public classes.
            if (typeof(T).IsNestedPublic || typeof(T).IsPublic)
            {
                if (ReferenceEquals(_mappedType, null))
                    CreateClonedTypeWithAttributes();

                return _mappedType;
            }
            throw new NotSupportedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IMappedType MapFromInstance(T data)
        {
            if (TryAndLoadClonedTypeAssembly())
            {
                object result = _objectsMapperFrom.Map(data);
                return (IMappedType)result;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public T MapToInstance(IMappedType data)
        {
            if (TryAndLoadClonedTypeAssembly())
            {
                object result = _objectsMapperTo.Map(data);
                return (T)result;
            }

            return default(T);
        }

        #endregion

        #region Non Public Members
        private Type TypeOfActualObject
        {
            get
            {
                if (_type == null) _type = typeof(T);
                return _type;
            }
        }


        /// <summary>
        /// Creates a new type based on T , marks each public property of T
        /// with ProtoMember, DataMember attributes. Also marks the classes
        /// as ProtoContract or DataContract attributes.
        /// </summary>
        private void CreateClonedTypeWithAttributes()
        {
            string typeName = TypeOfActualObject.AssemblyQualifiedName ?? TypeOfActualObject.Name;
            _assemblyName = Convert.ToBase64String(Encoding.ASCII.GetBytes(typeName));

            if (!TryAndLoadClonedTypeAssembly())
            {
                TypeBuilder typeBuilder = CreateTypeBuilder();
                var findProps = from propertyInfo in TypeOfActualObject.GetProperties()
                                    .Where(iPropInfo => SystemTypes.fclDlls.Contains(iPropInfo.PropertyType.Assembly.FullName) &&
                                                        iPropInfo.MemberType == MemberTypes.Property &&
                                                        iPropInfo.GetSetMethod() != null)
                                select propertyInfo;

                List<PropertyInfo> props = findProps.ToList();
                int protoMemberId = 1;

                foreach (PropertyInfo info in props)
                {
                    FieldBuilder fieldBuilder = typeBuilder.DefineField(string.Format("m_{0}", info.Name),
                                                                        info.PropertyType,
                                                                        FieldAttributes.Private);

                    string getterName = string.Concat("get_", info.Name);
                    string setterName = string.Concat("set_", info.Name);

                    PropertyBuilder propBuilder
                        = typeBuilder.DefineProperty(info.Name,
                                                     PropertyAttributes.HasDefault,
                                                     CallingConventions.Standard,
                                                     info.PropertyType,
                                                     null);

                    //Apply ProtoMember Attribute
                    Type[] ctorParams = new[] { typeof(Int32) };
                    ConstructorInfo protoMbrCtorInfo = typeof(ProtoMemberAttribute).GetConstructor(ctorParams);
                    CustomAttributeBuilder protoMbrAttbBuilder = new CustomAttributeBuilder(protoMbrCtorInfo, new object[] { protoMemberId++ });

                    propBuilder.SetCustomAttribute(protoMbrAttbBuilder);

                    //Apply Data Member Attribute
                    ConstructorInfo classCtorInfo = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);

                    CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(classCtorInfo, new object[] { });

                    propBuilder.SetCustomAttribute(attributeBuilder);

                    MethodBuilder getPropMethdBldr = typeBuilder.DefineMethod(getterName, GetSetAttr, info.PropertyType, Type.EmptyTypes);

                    ILGenerator getterIL = getPropMethdBldr.GetILGenerator();

                    getterIL.Emit(OpCodes.Ldarg_0);
                    getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
                    getterIL.Emit(OpCodes.Ret);

                    MethodBuilder setPropMethdBldr = typeBuilder.DefineMethod(setterName, GetSetAttr, null, new[] { info.PropertyType });

                    ILGenerator setterIL = setPropMethdBldr.GetILGenerator();
                    setterIL.Emit(OpCodes.Ldarg_0);
                    setterIL.Emit(OpCodes.Ldarg_1);
                    setterIL.Emit(OpCodes.Stfld, fieldBuilder);
                    setterIL.Emit(OpCodes.Ret);

                    propBuilder.SetGetMethod(getPropMethdBldr);
                    propBuilder.SetSetMethod(setPropMethdBldr);
                }

                typeBuilder.CreateType();
                //save the assembly in a preconfigured dir..
                //before we try to create it, we can load all assemblies from the directory
                _assemblyBuilder.Save(_assemblyName + ".dll");
            }

            if (TryAndLoadClonedTypeAssembly())
            {
                _mappedType = _assembly.GetTypes()[0];
                _objectsMapperFrom = ObjectMapperManager.DefaultInstance.GetMapperImpl(TypeOfActualObject, _mappedType,
                                                                                       DefaultMapConfig.Instance);
                _objectsMapperTo = ObjectMapperManager.DefaultInstance.GetMapperImpl(_mappedType, TypeOfActualObject,
                                                                                     DefaultMapConfig.Instance);
            }
        }

        private TypeBuilder CreateTypeBuilder()
        {
            string className = Guid.NewGuid().ToString();
            string moduleName = String.Format("{0}.dll", _assemblyName);

            // Get current currentDomain.
            AppDomain currentDomain = AppDomain.CurrentDomain;
            // Create assembly in current currentDomain.
            _name = new AssemblyName { Name = _assemblyName };
            //name.CodeBase =clonedTypesAssemblyPath;

            _assemblyBuilder = currentDomain.DefineDynamicAssembly(_name, AssemblyBuilderAccess.Save);

            // create a module in the assembly
            ModuleBuilder moduleBuilder = _assemblyBuilder.DefineDynamicModule(moduleName, true);
            // create a type in the module
            TypeBuilder typeBuilder
                = moduleBuilder.DefineType(String.Format("{0}.{1}", _assemblyName, className),
                                           TypeAttributes.Class | TypeAttributes.Public,
                                           null, new[] { typeof(IMappedType) });

            typeBuilder.AddInterfaceImplementation(typeof(IMappedType));

            //Apply DataContract Attribute
            ConstructorInfo dataContractCtorInfo = typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(dataContractCtorInfo, new object[] { });

            typeBuilder.SetCustomAttribute(attributeBuilder);

            //Apply ProtoContractAttribute
            ConstructorInfo protoContractInfo = typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder protoBuffClassAttributeBuilder = new CustomAttributeBuilder(protoContractInfo, new object[] { });
            typeBuilder.SetCustomAttribute(protoBuffClassAttributeBuilder);

            return typeBuilder;
        }

        private bool TryAndLoadClonedTypeAssembly()
        {
            if (!_assemblyLoaded)
            {
                try
                {
                    //loadfile is deprecated but loadfrom is not working
                    _assembly = Assembly.LoadFile(String.Format(@"{0}\{1}.dll", AppDomain.CurrentDomain.BaseDirectory, _assemblyName));
                    _assemblyLoaded = true;
                }
                catch (Exception)
                {
                    _assemblyLoaded = false;
                }
            }
            return _assemblyLoaded;
        }
        #endregion
    }
}