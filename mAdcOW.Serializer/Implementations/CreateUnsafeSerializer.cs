using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CSharp;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Class which tries to create a ISerializeDeserialize based on pointer movement (unsafe).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CreateUnsafeSerializer<T>
    {
        private readonly Type _type = typeof(T);
        private int _addCount;
        private int _ptrSize = 8;
        private string _ptrType = "Int64";
        private int _size;

        public ISerializeDeserialize<T> GetSerializer()
        {
            if (!CanGetSize()) return null;
            ValueTypeCheck checker = new ValueTypeCheck(typeof(T));
            if (!checker.OnlyValueTypes())
            {
                return null;
            }
            CompilerResults res = CompileCode();
            if (res.Errors.Count > 0)
            {
                throw new SerializerException(res.Errors[0].ErrorText);
            }
            return (ISerializeDeserialize<T>)res.CompiledAssembly.CreateInstance("UnsafeConverter");
        }

        private bool CanGetSize()
        {
            try
            {
                _size = Marshal.SizeOf(typeof(T));
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        private CompilerResults CompileCode()
        {
            var providerOptions = new Dictionary<string, string>
                                      {
                                          {"CompilerVersion", "v3.5"}
                                      };
            CodeDomProvider provider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParameters = GetCompilerParameters();
            return provider.CompileAssemblyFromSource(compilerParameters, GenerateCode());
        }

        private string GenerateCode()
        {
            string typeFullName = _type.FullName.Replace('+', '.');

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();

            Type interfaceType = typeof(ISerializeDeserialize<T>);

            sb.AppendFormat("public class UnsafeConverter : {0}.ISerializeDeserialize<{1}>",
                            interfaceType.Namespace,
                            typeFullName);
            sb.Append("{");
            sb.AppendFormat("public bool CanSerializeType(){{return true;}}");

            ObjectToBytesCode(sb, typeFullName);
            BytesToObjectCode(sb, typeFullName);

            sb.Append("}");
            return sb.ToString();
        }

        private CompilerParameters GetCompilerParameters()
        {
            CompilerParameters cParameters = new CompilerParameters
                                                 {
                                                     GenerateInMemory = true,
                                                     GenerateExecutable = false,
                                                     TreatWarningsAsErrors = false,
                                                     IncludeDebugInformation = false,
                                                     CompilerOptions = "/optimize /unsafe"
                                                 };
            cParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            if(!_type.Assembly.FullName.StartsWith("mscorlib"))
                cParameters.ReferencedAssemblies.Add(_type.Assembly.Location);
            return cParameters;
        }

        private void BytesToObjectCode(StringBuilder sb, string typeFullName)
        {
            sb.AppendFormat("public unsafe {0} BytesToObject( byte[] bytes )", typeFullName);
            sb.Append("{");
            sb.Append(@"
                fixed (byte* srcPtr = &bytes[0])
                {");
            sb.AppendFormat("return *({0}*)srcPtr;", typeFullName);
            sb.Append("}}");
        }

        private void ObjectToBytesCode(StringBuilder sb, string typeFullName)
        {
            sb.AppendFormat("public unsafe byte[] ObjectToBytes({0} srcObject)", typeFullName);
            sb.Append("{");
            sb.AppendFormat("byte[] buffer = new byte[{0}];", _size);
            sb.Append(@"
                fixed (byte* destPtr = &buffer[0])
                {
                    ");
            sb.Append("byte* src = (byte*)&srcObject;");
            sb.Append("byte* dest = destPtr;");

            GenerateMethodBodyCode(sb);

            sb.Append(@"}                
                return buffer;}");
        }

        private void GenerateMethodBodyCode(StringBuilder sb)
        {
            _addCount = 0;
            int length = _size;
            do
            {
                MovePointers(sb);
                SetPointerLength(length);
                sb.AppendFormat(@"*(({0}*)dest+{1}) = *(({0}*)src+{1});", _ptrType, _addCount / _ptrSize);
                length -= _ptrSize;
                _addCount += _ptrSize;
            } while (length > 0);
        }

        private void MovePointers(StringBuilder sb)
        {
            int modifer = _addCount / _ptrSize;
            if (modifer >= _ptrSize)
            {
                sb.AppendFormat("dest += {0};", _addCount);
                sb.AppendFormat("src += {0};", _addCount);
                _addCount = 0;
            }
        }

        private void SetPointerLength(int length)
        {
            if (length >= 8)
            {
                _ptrSize = 8;
                _ptrType = "Int64";
            }
            else if (length >= 4)
            {
                _ptrSize = 4;
                _ptrType = "Int32";
            }
            else if (length >= 2)
            {
                _ptrSize = 2;
                _ptrType = "Int16";
            }
            else
            {
                _ptrSize = 1;
                _ptrType = "byte";
            }
        }
    }
}