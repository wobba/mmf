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

namespace mAdcOW.Serializer
{

    //TODO: Nested Types
    //Non Primitive properties
    public class Mapper<T> 
    {  
        //mapped type.
        static Type _mappedType = null;
        Type _type = null;


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
            object result = ObjectMapperManager.DefaultInstance.GetMapperImpl
                                                               (TypeOfActualObject,
                                                                _mappedType,
                                                                DefaultMapConfig.Instance).Map(data);

            return (IMappedType)result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public T MapToInstance(IMappedType data)
        {
            
            object result= ObjectMapperManager.DefaultInstance.GetMapperImpl
                                                               (_mappedType, 
                                                                TypeOfActualObject, 
                                                                DefaultMapConfig.Instance).Map(data);
            return (T)result;
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
        //TODO: need to check if field also be generated.               
        //TODO: Test, benchmark, update demo app,update dictionary MapperCache..
        private void CreateClonedTypeWithAttributes()
        {

            TypeBuilder typeBuilder = CreateTypeBuilder();
        
            var findProps = from propertyInfo in TypeOfActualObject.GetProperties()
                             .Where(iPropInfo => iPropInfo.PropertyType.IsPrimitive  &&
                                    iPropInfo.MemberType == MemberTypes.Property &&
                                    iPropInfo.GetSetMethod()!= null )
                             select propertyInfo;
        
            List<PropertyInfo> props = findProps.ToList();

            MethodAttributes getSetAttr
                    = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            int protoMemberId = 1;

            foreach (PropertyInfo info in props)
            {

                FieldBuilder fieldBuilder = typeBuilder.DefineField("m_"+info.Name,
                                                        info.PropertyType,
                                                        FieldAttributes.Private);


                string getterName = string.Concat("get_", info.Name);
                string setterName = string.Concat("set_", info.Name);

                PropertyBuilder propBuilder
                    = typeBuilder.DefineProperty(info.Name,
                                  PropertyAttributes.HasDefault,
                                  info.PropertyType,
                                       null);

                //Apply Data Member Attribute
                ConstructorInfo classCtorInfo = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);

                CustomAttributeBuilder attributeBuilder
                    = new CustomAttributeBuilder(classCtorInfo, new object[] {});
                                                              
                propBuilder.SetCustomAttribute(attributeBuilder);

                //Apply ProtoMember Attribute
                Type[] ctorParams = new Type[] { typeof(Int32) };
                ConstructorInfo protoMbrCtorInfo = typeof(ProtoMemberAttribute).GetConstructor(ctorParams);

                CustomAttributeBuilder protoMbrAttbBuilder
                    = new CustomAttributeBuilder(protoMbrCtorInfo,new object[]{ protoMemberId++});

                propBuilder.SetCustomAttribute(protoMbrAttbBuilder);
                
                
                MethodBuilder getPropMethdBldr 
                    = typeBuilder.DefineMethod(getterName,getSetAttr,info.PropertyType,Type.EmptyTypes);

                ILGenerator getterIL = getPropMethdBldr.GetILGenerator();

                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getterIL.Emit(OpCodes.Ret);


                MethodBuilder setPropMethdBldr 
                    = typeBuilder.DefineMethod(setterName,getSetAttr,null,new Type[] { info.PropertyType });

                ILGenerator setterIL = setPropMethdBldr.GetILGenerator();

                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, fieldBuilder);
                setterIL.Emit(OpCodes.Ret);

                propBuilder.SetGetMethod(getPropMethdBldr);
                propBuilder.SetSetMethod(setPropMethdBldr);

            }

            _mappedType = typeBuilder.CreateType();
            //save the assembly in a preconfigured dir..
            //before we try to create it, we can load all assemblies from the directory
        }

       

        private TypeBuilder CreateTypeBuilder()
        {
            string assemblyName = Guid.NewGuid().ToString();
            string moduleName = TypeOfActualObject.Name;
            string className = TypeOfActualObject.Name;

            // Get current currentDomain.
            AppDomain currentDomain = AppDomain.CurrentDomain;
            // Create assembly in current currentDomain.
            AssemblyName name = new AssemblyName();
            name.Name = assemblyName;
            AssemblyBuilder assemblyBuilder 
                    = currentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

            // create a module in the assembly
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            // create a type in the module
            TypeBuilder typeBuilder 
                    = moduleBuilder.DefineType(className, TypeAttributes.Class| TypeAttributes.Public,null, new Type[]{typeof(IMappedType)});
            
            typeBuilder.AddInterfaceImplementation(typeof(IMappedType));
            

            //Apply DataContract Attribute
            ConstructorInfo dataContractCtorInfo 
                    = typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder attributeBuilder 
                    = new CustomAttributeBuilder(dataContractCtorInfo,new object[]{});
                                                             
            typeBuilder.SetCustomAttribute(attributeBuilder);

            //Apply ProtoContractAttribute
            ConstructorInfo protoContractInfo 
                    = typeof(ProtoContractAttribute).GetConstructor(Type.EmptyTypes);

            CustomAttributeBuilder protoBuffClassAttributeBuilder
                    = new CustomAttributeBuilder(protoContractInfo, new object[] {});
         
            return typeBuilder;
        }

        #endregion

    }
}
