using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using mAdcOW.DataStructures;
using mAdcOW.DataStructures.DictionaryBacking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.DataStructures.Test
{
    /// <summary>
    /// Summary description for DictionaryTestUnknownSizeBacking
    /// </summary>
    [TestClass]
    public class DictionaryTestUnknownSizeBacking
    {
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

        class Customer
        {
            public string Name { get; set; }

            public override int GetHashCode()
            {
                return 1;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Customer)) return false;
                return string.Equals(Name, ((Customer)obj).Name);
            }
        }

        [TestMethod]
        public void AddValues_VerifyValues()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<string, string> backingFile = new BackingUnknownSize<string, string>(path, 2000000);
            mAdcOW.DataStructures.Dictionary<string, string> dict = new mAdcOW.DataStructures.Dictionary<string, string>(backingFile);

            string prevKey = null;
            string prevVal = null;
            for (int i = 0; i < 500000; i++)
            {
                string key = Guid.NewGuid().ToString();
                string value = Guid.NewGuid().ToString();
                dict.Add(key, value);

                if (prevKey != null)
                {
                    string result = dict[prevKey];
                    Assert.AreEqual(prevVal, result);
                }
                prevKey = key;
                prevVal = value;
            }
        }

        [TestMethod]
        public void AddTwoValues_CheckConsistency()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<Customer, string> backingFile = new BackingUnknownSize<Customer, string>(path, 100);
            mAdcOW.DataStructures.Dictionary<Customer, string> dict = new mAdcOW.DataStructures.Dictionary<Customer, string>(backingFile);

            Customer c1 = new Customer { Name = "Mikael" };
            Customer c2 = new Customer { Name = "Svenson" };

            dict.Add(c1, "test");
            dict.Add(c2, "test2");
            string result = dict[c1];
            Assert.AreEqual("test", result);
            result = dict[c2];
            Assert.AreEqual("test2", result);
        }

        [TestMethod]
        public void AddThreeItems_RemoveMiddleItem_CheckConsistency()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<Customer, string> backingFile = new BackingUnknownSize<Customer, string>(path, 100);
            mAdcOW.DataStructures.Dictionary<Customer, string> dict = new mAdcOW.DataStructures.Dictionary<Customer, string>(backingFile);

            Customer c1 = new Customer { Name = "Mikael" };
            Customer c2 = new Customer { Name = "Svenson" };
            Customer c3 = new Customer { Name = "Boss" };

            dict.Add(c1, "test");
            dict.Add(c2, "test2");
            dict.Add(c3, "test3");

            var result = dict.Remove(c2);
            Assert.IsTrue(result);
            result = dict.Remove(c2);
            Assert.IsFalse(result);
            dict.Add(c2, "test2");
            result = dict.Remove(c2);
            Assert.IsTrue(result);

            var res2 = dict[c3];
            Assert.AreEqual("test3", res2);
        }

        [TestMethod]
        public void AddThreeItems_RemoveFirstItem_CheckConsistency()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<Customer, string> backingFile = new BackingUnknownSize<Customer, string>(path, 100);
            mAdcOW.DataStructures.Dictionary<Customer, string> dict = new mAdcOW.DataStructures.Dictionary<Customer, string>(backingFile);

            Customer c1 = new Customer { Name = "Mikael" };
            Customer c2 = new Customer { Name = "Svenson" };
            Customer c3 = new Customer { Name = "Boss" };

            dict.Add(c1, "test");

            var result = dict.Remove(c1);
            Assert.IsTrue(result);

            dict.Add(c2, "test2");
            dict.Add(c3, "test3");
            dict.Add(c1, "test");

            result = dict.Remove(c1);
            Assert.IsTrue(result);
            result = dict.Remove(c1);
            Assert.IsFalse(result);
            dict.Add(c1, "test");
            result = dict.Remove(c1);
            Assert.IsTrue(result);

            var res2 = dict[c3];
            Assert.AreEqual("test3", res2);
        }

        [TestMethod]
        public void IterateAllItems_CheckConsistency()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<Customer, string> backingFile = new BackingUnknownSize<Customer, string>(path, 100);
            mAdcOW.DataStructures.Dictionary<Customer, string> dict = new mAdcOW.DataStructures.Dictionary<Customer, string>(backingFile);

            Customer c1 = new Customer { Name = "Mikael" };
            Customer c2 = new Customer { Name = "Svenson" };
            Customer c3 = new Customer { Name = "Boss" };

            dict.Add(c1, "Mikael");
            dict.Add(c2, "Svenson");
            dict.Add(c3, "Boss");

            int count = 0;
            foreach (KeyValuePair<Customer, string> pair in dict)
            {
                Assert.AreEqual(pair.Key.Name, pair.Value);
                count++;
            }
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public void IterateAllItems_CheckConsistency2()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<int, int> backingFile = new BackingUnknownSize<int, int>(path, 100);
            mAdcOW.DataStructures.Dictionary<int, int> dict = new mAdcOW.DataStructures.Dictionary<int, int>(backingFile);

            dict.Add(1, 1);
            dict.Add(2, 2);
            dict.Add(3, 3);

            int count = 0;
            foreach (KeyValuePair<int, int> pair in dict)
            {
                Assert.AreEqual(pair.Key, pair.Value);
                count++;
            }
            Assert.AreEqual(3, count);
        }
    }
}
