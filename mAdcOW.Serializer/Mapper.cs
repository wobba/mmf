using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmitMapper;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using EmitMapper.MappingConfiguration;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using ProtoBuf;
using System.Security.Policy;

namespace mAdcOW.Serializer
{

    //TODO: Nested Types
    //Non Primitive properties
    public class Mapper<T> 
    {  
        //mapped type.
        static Type _mappedType = null;
        Type _type = null;
        static bool assemblyLoaded = false;


        #region Public Properties

        /// <summary>
        /// Gets the appropriate cloned type for T, after marking the
        /// public properties with the right attributes
        /// </summary>
        /// <returns></returns>
        public Type GetMappedType()
        {

            if (Type.ReferenceEquals(_mappedType, null))
                CreateClonedTypeWithAttributes();

            return _mappedType;
             
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

                object result = ObjectMapperManager.DefaultInstance.GetMapperImpl
                                                                   (TypeOfActualObject,
                                                                    _mappedType,
                                                                    DefaultMapConfig.Instance).Map(data);
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

                object result = ObjectMapperManager.DefaultInstance.GetMapperImpl
                                                                   (_mappedType,
                                                                    TypeOfActualObject,
                                                                    DefaultMapConfig.Instance).Map(data);
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
                if (_type == null)
                    _type = typeof(T);

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
            assemblyName = TypeOfActualObject.GetHashCode().ToString();
            if (!TryAndLoadClonedTypeAssembly())
            {
                TypeBuilder typeBuilder = CreateTypeBuilder();

                var findProps = from propertyInfo in TypeOfActualObject.GetProperties()
                                 .Where(iPropInfo => iPropInfo.PropertyType.IsPrimitive &&
                                        iPropInfo.MemberType == MemberTypes.Property &&
                                        iPropInfo.GetSetMethod() != null)
                                select propertyInfo;

                List<PropertyInfo> props = findProps.ToList();

                MethodAttributes getSetAttr
                        = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

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
                    Type[] ctorParams = new Type[] { typeof(Int32) };
                    ConstructorInfo protoMbrCtorInfo = typeof(ProtoMemberAttribute).GetConstructor(ctorParams);

                    CustomAttributeBuilder protoMbrAttbBuilder
                        = new CustomAttributeBuilder(protoMbrCtorInfo, new object[] { protoMemberId++ });

                    propBuilder.SetCustomAttribute(protoMbrAttbBuilder);

                    //Apply Data Member Attribute
                    ConstructorInfo classCtorInfo = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);

                    CustomAttributeBuilder attributeBuilder
                        = new CustomAttributeBuilder(classCtorInfo, new object[] { });

                    propBuilder.SetCustomAttribute(attributeBuilder);

                    


                    MethodBuilder getPropMethdBldr
                        = typeBuilder.DefineMethod(getterName, getSetAttr, info.PropertyType, Type.EmptyTypes);

                    ILGenerator getterIL = getPropMethdBldr.GetILGenerator();

                    getterIL.Emit(OpCodes.Ldarg_0);
                    getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
                    getterIL.Emit(OpCodes.Ret);


                    MethodBuilder setPropMethdBldr
                        = typeBuilder.DefineMethod(setterName, getSetAttr, null, new Type[] { info.PropertyType });

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
                assemblyBuilder.Save(assemblyName + ".dll");
            }

            if (TryAndLoadClonedTypeAssembly())
            {
                _mappedType = assembly.GetTypes()[0];
            }
           
            
        }

        Assembly assembly;
        string assemblyName = string.Empty;
        AssemblyBuilder assemblyBuilder;
        AssemblyName name;
        string clonedTypesAssemblyPath = "C:/Assemblies";
        string clonedTypeName = string.Empty;

        private TypeBuilder CreateTypeBuilder()
        {

            string className = Guid.NewGuid().ToString();

            string moduleName = String.Format("{0}.dll",assemblyName);
            

            // Get current currentDomain.
            AppDomain currentDomain = AppDomain.CurrentDomain;
            // Create assembly in current currentDomain.
            name = new AssemblyName();
            name.Name = assemblyName;
            //name.CodeBase =clonedTypesAssemblyPath;

            assemblyBuilder 
                    = currentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);

            // create a module in the assembly
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName,true);
            // create a type in the module
            TypeBuilder typeBuilder 
                    = moduleBuilder.DefineType(String.Format("{0}.{1}",assemblyName, className), 
                                               TypeAttributes.Class| TypeAttributes.Public ,
                                               null, new Type[] {typeof(IMappedType)});
            
            typeBuilder.AddInterfaceImplementation(typeof(IMappedType));

            //Apply DataContract Attribute
            ConstructorInfo dataContractCtorInfo 
                    = typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder attributeBuilder 
                    = new CustomAttributeBuilder(dataContractCtorInfo,new object[]{ });
                                                             
            typeBuilder.SetCustomAttribute(attributeBuilder);

            //Apply ProtoContractAttribute
            ConstructorInfo protoContractInfo 
                    = typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder protoBuffClassAttributeBuilder
                    = new CustomAttributeBuilder(protoContractInfo, new object[] { });
            typeBuilder.SetCustomAttribute(protoBuffClassAttributeBuilder);
         
            return typeBuilder;
        }

        private bool TryAndLoadClonedTypeAssembly()
        {
            if (!assemblyLoaded)
            {
                try
                {
                    //loadfile is deprecated but loadfrom is not working
                    assembly = Assembly.LoadFile(String.Format("{0}{1}.dll", AppDomain.CurrentDomain.BaseDirectory,assemblyName));
                    assemblyLoaded = true;

                }
                catch (Exception ex)
                {
                    assemblyLoaded = false;
                }
            }
            return assemblyLoaded;
        }

        #endregion

    }
}
