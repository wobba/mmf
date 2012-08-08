using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace mAdcOW.Serializer.Test
{
    /// <summary>
    /// Summary description for PerformanceTests
    /// </summary>
    [TestFixture]
    public class PerformanceTestsClass
    {
        private const int Iterations = 100000;

        

        ///// <summary>
        /////Gets or sets the test context which provides
        /////information about and functionality for the current test run.
        /////</summary>
        //public TestContext TestContext
        //{
        //    get { return testContextInstance; }
        //    set { testContextInstance = value; }
        //}

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

        [Test]
        public void Serialize()
        {
            TestFixture test = CreateSerializationObject();

            var factory = new Factory<TestFixture>();
            var serializers = factory.GetValidSerializers();
            foreach (var serializer in serializers)
            {
                BenchmarkSerializeMethod(serializer, test);
            }
        }

        [Test]
        public void Deserialize()
        {
            TestFixture test = CreateSerializationObject();

            var factory = new Factory<TestFixture>();
            var serializers = factory.GetValidSerializers();
            foreach (var serializer in serializers)
            {
                BenchmarkDeserializeMethod(serializer, test);
            }
        }

        #region Serialize

        public void BenchmarkSerializeMethod<T>(ISerializeDeserialize<T> serializer, object instance)
        {
            serializer.ObjectToBytes((T) instance);

            Stopwatch timed = new Stopwatch();
            timed.Start();

            byte[] bytes = null;
            for (int x = 0; x < Iterations; x++)
            {
                bytes = serializer.ObjectToBytes((T) instance);
            }

            timed.Stop();

            Trace.WriteLine("Serialize method: "+ serializer.GetType().Name);
            Trace.WriteLine(timed.ElapsedMilliseconds + " ms");
            Trace.WriteLine("Data size: " + bytes.Length);
            Trace.WriteLine("");
        }

        private TestFixture CreateSerializationObject()
        {
            TestFixture test = new TestFixture();

            test.dictionary = new Dictionary<string, int> {{"Val & asd1", 1}, {"Val2 & asd1", 3}, {"Val3 & asd1", 4}};


            test.Address1.Street = "fff Street";
            test.Address1.Entered = DateTime.Now.AddDays(20);

            test.BigNumber = 34123123123.121M;
            test.Now = DateTime.Now.AddHours(1);
            test.strings = new List<string> {null, "Markus egger ]><[, (2nd)", null};

            Address address = new Address();
            address.Entered = DateTime.Now.AddDays(-1);
            address.Street = "\u001farray\u003caddress";

            test.Addresses.Add(address);

            address = new Address();
            address.Entered = DateTime.Now.AddDays(-2);
            address.Street = "array 2 address";
            test.Addresses.Add(address);
            return test;
        }

        #endregion

        #region Deserialize

        public void BenchmarkDeserializeMethod<T>(ISerializeDeserialize<T> serializer, object instance)
        {
            var bytes = serializer.ObjectToBytes((T) instance);
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

    [Serializable]
    //[DataContract]
    public class TestFixture
    {
        //[DataMember(Order = 1)]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private string _Name = "Rick";

        //[DataMember(Order = 2)]
        public DateTime Now
        {
            get { return _Now; }
            set { _Now = value; }
        }

        private DateTime _Now = DateTime.Now;

        //[DataMember(Order = 3)]
        public decimal BigNumber
        {
            get { return _BigNumber; }
            set { _BigNumber = value; }
        }

        private decimal _BigNumber = 1212121.22M;

        //[DataMember(Order = 4)]
        public Address Address1
        {
            get { return _Address1; }
            set { _Address1 = value; }
        }

        private Address _Address1 = new Address();


        //[DataMember(Order = 5)]
        public List<Address> Addresses
        {
            get { return _Addresses; }
            set { _Addresses = value; }
        }

        private List<Address> _Addresses = new List<Address>();

        //[DataMember(Order = 6)]
        public List<string> strings = new List<string>();

        //[DataMember(Order = 7)]
        public Dictionary<string, int> dictionary = new Dictionary<string, int>();
    }

    [Serializable]
    [DataContract]
    public class Address
    {
        [DataMember(Order = 1)]
        public string Street
        {
            get { return _street; }
            set { _street = value; }
        }

        private string _street = "32 Kaiea";

        [DataMember(Order = 2)]
        public string Phone
        {
            get { return _Phone; }
            set { _Phone = value; }
        }

        private string _Phone = "(503) 814-6335";

        [DataMember(Order = 3)]
        public DateTime Entered
        {
            get { return _Entered; }
            set { _Entered = value; }
        }

        private DateTime _Entered = DateTime.Parse("01/01/2007", CultureInfo.CurrentCulture.DateTimeFormat);
    }

    #endregion
}