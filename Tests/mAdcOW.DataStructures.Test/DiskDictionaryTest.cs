﻿// The following code was generated by Microsoft Visual Studio 2005.
// The test owner should check each test for validity.
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IntelliSearch.Utils.Test
{
    /// <summary>
    ///This is a test class for IntelliSearch.Utils.DiskDictionary.DiskDictionary&lt;KEY, VALUE&gt; and is intended
    ///to contain all IntelliSearch.Utils.DiskDictionary.DiskDictionary&lt;KEY, VALUE&gt; Unit Tests
    ///</summary>
    [TestClass()]
    public class DiskDictionaryTest
    {
        class TestClass
        {
            private string _name = "Mikael";
            private string _address = "EckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckersEckers";
            byte[] _data = new byte[20];

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public string Address
            {
                get { return _address; }
                set { _address = value; }
            }
        }

        class KeyClass : IComparable
        {
            private string _key = "lala";

            public string Key
            {
                get { return _key; }
                set { _key = value; }
            }

            public KeyClass()
            {
            }

            public KeyClass( string key )
            {
                _key = key;
            }

            public override int GetHashCode()
            {
                return 1;
            }

            public override bool Equals(object obj)
            {
                return _key.Equals(((KeyClass) obj).Key);
            }

            #region IComparable Members

            public int CompareTo(object obj)
            {
                return _key.CompareTo(((KeyClass) obj).Key);
            }

            #endregion
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        #region Additional test attributes

        private const int COUNT = 50000;
        static DiskDictionary<string, TestClass> _dict = new DiskDictionary<string, TestClass>(COUNT);
        
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            for (int i = 0; i < COUNT; i++)
            {
                _dict.Add(i.ToString(), new TestClass());
            }
        }
        
        //Use ClassCleanup to run code after all tests in a class have run
        //
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Add (KEY, VALUE)
        ///</summary>
        [TestMethod()]
        public void DataTest()
        {
            TestClass checkObject;

            // retrieve the first item, and use for check with the rest
            Assert.IsTrue(_dict.TryGetValue((0).ToString(), out checkObject));
            // retrieve the last item, and use for check with the rest
            Assert.IsTrue(_dict.TryGetValue((COUNT-1).ToString(), out checkObject));

            for (int i = 0; i < COUNT; i++)
            {
                TestClass abc = _dict[i.ToString()];
                if( !abc.Name.Equals( checkObject.Name  ) )
                {
                    Assert.Fail( "Object name differs" );
                }
                if (!abc.Address.Equals(checkObject.Address))
                {
                    Assert.Fail("Object address differs");
                }
            }
        }

        [TestMethod()]
        public void AccessNonExistingKey()
        {
            TestClass checkObject;
            // try to retrieve an item which don't exist
            Assert.IsFalse(_dict.TryGetValue("wrongkey", out checkObject));
        }

        [TestMethod()]
        public void RemoveKey()
        {
            TestClass checkObject;
            // remove a key
            int key = COUNT/2;
            Assert.IsTrue(_dict.Remove(key.ToString()));
            // should fail to retrive it
            Assert.IsFalse(_dict.TryGetValue(key.ToString(), out checkObject));
            _dict[key.ToString()] = new TestClass();
        }

        [TestMethod()]
        public void AddDuplicateKeys()
        {
            _dict.Add("unique", new TestClass());
            try
            {
                _dict.Add("unique", new TestClass());
                Assert.Fail("Expected ArgumentException");
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod()]
        public void AccessUnknownKey()
        {            
            try
            {
                TestClass tc = _dict["nokey"];
                Assert.Fail("Expected KeyNotFoundException");
            }
            catch (KeyNotFoundException)
            {
            }
        }

        [TestMethod()]
        public void EqualHashDifferentKey()
        {
            KeyClass key1 = new KeyClass("a");
            KeyClass key2 = new KeyClass("b");

            DiskDictionary<KeyClass,int> dict = new DiskDictionary<KeyClass, int>();
            dict.Add(key1, 2);
            dict.Add(key2, 3);

            Assert.IsTrue( dict[key1] == 2 );
            Assert.IsTrue( dict[key2] == 3 );

            Assert.IsTrue(dict.Remove(key1));
            Assert.IsTrue(dict.Remove(key2));
        }

        [TestMethod()]
        public void IterateKeys()
        {
            foreach (string s in _dict.Keys)
            {
                TestClass x = _dict[s];
                Assert.IsTrue( x != null );
            }
        }

        [TestMethod()]
        public void IterateValues()
        {
            TestClass c = new TestClass();
            foreach (TestClass value in _dict.Values)
            {
                Assert.IsTrue(value.Name.Equals(c.Name));
            }
        }

        [TestMethod()]
        public void IterateKeyValuePair()
        {
            TestClass c = new TestClass();
            foreach (KeyValuePair<string, TestClass> pair in _dict)
            {
                Assert.IsTrue( pair.Value.Name.Equals( c.Name ) );
            }
        }

        [TestMethod()]
        public void AddKPTest()
        {
            KeyValuePair<string, TestClass> kp = new KeyValuePair<string, TestClass>(COUNT.ToString(), new TestClass());
            _dict.Add(kp);
        }
    }
}