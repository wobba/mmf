using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.Serializer.Test
{
    /// <summary>
    /// Summary description for PerformanceTests
    /// </summary>
    [TestClass]
    public class PerformanceTestsStruct
    {
        private const int Iterations = 100000;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        [TestMethod]
        public void Serialize()
        {
            Coordinate test = CreateSerializationObject();

            var factory = new Factory<Coordinate>();
            var serializers = factory.GetValidSerializers();
            foreach (var serializer in serializers)
            {
                BenchmarkSerializeMethod(serializer, test);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            Coordinate test = CreateSerializationObject();

            var factory = new Factory<Coordinate>();
            var serializers = factory.GetValidSerializers();
            foreach (var serializer in serializers)
            {
                BenchmarkDeserializeMethod(serializer, test);
            }
        }

        #region Serialize

        public void BenchmarkSerializeMethod<T>(ISerializeDeserialize<T> serializer, object instance)
        {
            serializer.ObjectToBytes((T)instance);

            Stopwatch timed = new Stopwatch();
            timed.Start();

            byte[] bytes = null;
            for (int x = 0; x < Iterations; x++)
            {
                bytes = serializer.ObjectToBytes((T)instance);
            }

            timed.Stop();

            Trace.WriteLine("Serialize method: " + serializer.GetType().Name);
            Trace.WriteLine(timed.ElapsedMilliseconds + " ms");
            Trace.WriteLine("Data size: " + bytes.Length);
            Trace.WriteLine("");
        }

        private Coordinate CreateSerializationObject()
        {
            Coordinate test = new Coordinate();
            test.X = 123.12F;
            test.Y = 124.12F;
            test.Z = 1256.12F;
            test.Focus = 34123123123.121M;

            Payload payload = new Payload();
            payload.Data = 12;
            payload.Version = 2;

            test.Payload = payload;

            return test;
        }

        #endregion

        #region Deserialize

        public void BenchmarkDeserializeMethod<T>(ISerializeDeserialize<T> serializer, object instance)
        {
            var bytes = serializer.ObjectToBytes((T)instance);
            serializer.BytesToObject(bytes);

            Stopwatch timed = new Stopwatch();
            timed.Start();

            T value = default(T);
            for (int x = 0; x < Iterations; x++)
            {
                value = serializer.BytesToObject(bytes);
            }

            timed.Stop();

            Trace.WriteLine("Deserialize method: " + serializer.GetType().Name);
            Trace.WriteLine(timed.ElapsedMilliseconds + " ms");
            Trace.WriteLine(value);
            Trace.WriteLine("");
        }

        #endregion
    }

    #region Classes

    [DataContract]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Coordinate
    {
        [DataMember(Order = 1)]
        public float X;
        [DataMember(Order = 2)]
        public float Y;
        [DataMember(Order = 3)]
        public float Z;
        [DataMember(Order = 4)]
        [MarshalAs(UnmanagedType.Currency)]
        public decimal Focus;
        [DataMember(Order = 5)]
        [MarshalAs(UnmanagedType.Struct)]
        public Payload Payload;

    }

    [DataContract]
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 113)]
    public struct Payload
    {
        [DataMember(Order = 1)]
        public byte Version;
        [DataMember(Order = 2)]
        public byte Data;
    }
    #endregion
}