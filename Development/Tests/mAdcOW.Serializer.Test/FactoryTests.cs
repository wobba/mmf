using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.Serializer.Test
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class FactoryTests
    {
        #region test objects

        #region Nested type: SimpleStruct
        public struct SimpleStruct
        {
            public bool Age;
            public byte False;
            public byte La;
            public Int64 Num;

            public SimpleStruct(bool a)
            {
                Num = 123123123;
                Age = true;
                False = 56;
                La = 12;
            }
        }
        #endregion

        #region Nested type: UnknownSizeClass
        public class UnknownSizeClass
        {
            public bool Age;
            public byte False;
            public byte La;
            public Int64 Num;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)] public char[] Yjoha;
            [MarshalAs(UnmanagedType.LPStr)] public string TestString;            
        }

        public class NonMarshalledClass
        {
            public bool Age;
            public byte False;
            public byte La;
            public Int64 Num;
            public char[] Yjoha;
            public string TestString;
        }
        #endregion

        #region Nested type: UnknownSizeStruct

        public struct UnknownSizeStruct
        {
            public bool Age;
            public byte False;
            public byte La;
            public Int64 Num;
            [MarshalAs(UnmanagedType.LPStr)] public string TestString;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)] public char[] Yjoha;

            public UnknownSizeStruct(bool a)
            {
                Num = 123123123;
                Age = true;
                False = 56;
                La = 12;
                Yjoha = "laa".ToCharArray();
                TestString = "a";
            }
        }

        #endregion

        #region Nested type: DataPackage
        public class DataPackage : Dictionary<string, object[]>
        {
            string _tableName;

            public string TableName
            {
                get { return _tableName; }
                set { _tableName = value; }
            }

            int _codenumber;

            public int Codenumber
            {
                get { return _codenumber; }
                set { _codenumber = value; }
            }
        }
        #endregion

        #endregion

        [TestMethod]
        public void When_sending_sending_in_an_explict_struct_it_should_return_an_unsafe_serializer()
        {
            Factory<SimpleStruct> factory = new Factory<SimpleStruct>();
            var actual = factory.GetSerializer();
            Assert.AreEqual("UnsafeConverter", actual.GetType().Name);
        }

        [TestMethod]
        public void When_serializing_integers_with_unsafe_serializer_validate_results()
        {
            CreateUnsafeSerializer<int> creator = new CreateUnsafeSerializer<int>();
            var actual = creator.GerSerializer();
            var array = actual.ObjectToBytes(1);
            var array2 = actual.ObjectToBytes(2);
            
            Assert.AreEqual(1, actual.BytesToObject(array));
            Assert.AreEqual(2, actual.BytesToObject(array2));
        }

        [TestMethod]
        public void When_sending_in_an_unknown_size_struct_it_should_return_a_wcf_serializer()
        {
            Factory<UnknownSizeStruct> factory = new Factory<UnknownSizeStruct>();
            var actual = factory.GetSerializer();
            Assert.IsInstanceOfType(actual, typeof(WcfDataContractSerializer<UnknownSizeStruct>));
        }

        [TestMethod]
        public void When_sending_in_an_unserializable_class_it_should_return_an_altserialize_serializer()
        {
            Factory<UnknownSizeClass> factory = new Factory<UnknownSizeClass>();
            var actual = factory.GetSerializer();
            Assert.IsInstanceOfType(actual, typeof (AltSerialize<UnknownSizeClass>));
        }

        [TestMethod]
        public void When_sending_in_an_nonmarshalled_class_it_should_return_an_altserialize_serializer()
        {
            Factory<NonMarshalledClass> factory = new Factory<NonMarshalledClass>();
            var actual = factory.GetSerializer();
            Assert.IsInstanceOfType(actual, typeof(AltSerialize<NonMarshalledClass>));
        }

        [TestMethod]
        public void When_sending_in_an_extended_dictionary_it_should_return_altserialize_serializer()
        {
            Factory<DataPackage> factory = new Factory<DataPackage>();
            var actual = factory.GetSerializer();

            Assert.IsTrue(actual is AltSerialize<DataPackage> || actual is WcfDataContractSerializer<DataPackage>);
        }

        [TestMethod]
        public void When_sending_in_a_string_it_should_return_a_serializer()
        {
            Factory<string> factory = new Factory<string>();
            var actual = factory.GetSerializer();
            Assert.IsInstanceOfType(actual, typeof (ISerializeDeserialize<string>));
        }
    }
}