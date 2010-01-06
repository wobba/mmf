using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CSharp;


namespace mAdcOW.Serializer.Implementations
{
    public class ProtocolBufferSerializer<T> : ISerializeDeserialize<T>
    {
        public byte[] ObjectToBytes(T data)
        {
            MemoryStream byteStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(byteStream, data);
            byteStream.Position = 0;
            return byteStream.ToArray();
        }

        public T BytesToObject(byte[] bytes)
        {
            MemoryStream byteStream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.Deserialize<T>(byteStream);
        }

        public bool CanSerializeType()
        {
            var targetClass = new CodeTypeDeclaration("mmf" + typeof(T).Name);
            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public;
            targetClass.IsPartial = true; //partial so that genn'ed code can be safely modified
            targetClass.CustomAttributes.Add(new CodeAttributeDeclaration("System.Runtime.Serialization.DataContract"));
            targetClass.BaseTypes.Add(new CodeTypeReference { BaseType = typeof(T).FullName, Options = CodeTypeReferenceOptions.GlobalReference });

            var cctor = new CodeConstructor();
            cctor.Attributes = MemberAttributes.Public;
            targetClass.Members.Add(cctor);


            CompilerParameters cParameters = new CompilerParameters
                                     {
                                         GenerateInMemory = true,
                                         GenerateExecutable = false,
                                         TreatWarningsAsErrors = false,
                                         IncludeDebugInformation = false,
                                         CompilerOptions = "/optimize /unsafe"
                                     };
            cParameters.ReferencedAssemblies.Add(typeof (DataContractAttribute).Assembly.Location);
            cParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            cParameters.ReferencedAssemblies.Add(typeof(T).Assembly.Location);


            
            var providerOptions = new Dictionary<string, string>
                                      {
                                          {"CompilerVersion", "v3.5"}
                                      };
            CSharpCodeProvider codeProvider = new CSharpCodeProvider(providerOptions);

            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace nameSpace = new CodeNamespace("mmfproxy");
            nameSpace.Types.Add(targetClass);
            compileUnit.Namespaces.Add(nameSpace);
            //StringWriter writer = new StringWriter();
            //codeProvider.GenerateCodeFromCompileUnit(compileUnit, writer, new System.CodeDom.Compiler.CodeGeneratorOptions());
            var res = codeProvider.CompileAssemblyFromDom(cParameters, compileUnit);
            if (res.Errors.Count > 0)
            {
                throw new SerializerException(res.Errors[0].ErrorText);
            }

            Type proxyType = res.CompiledAssembly.GetType("mmfproxy." + targetClass.Name);

            try
            {
                object[] args = null;
                if (typeof(T) == typeof(string))
                {
                    args = new object[] { new[] { 'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't' } };
                }
                T classInstance = (T)Activator.CreateInstance(proxyType, args);
                byte[] bytes = ObjectToBytes(classInstance);
                BytesToObject(bytes);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }
}