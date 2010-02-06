#region

using System;
using mAdcOW.DiskStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DiskStructuresTest
{
    /// <summary>
    ///This is a test class for MemoryMappedArray and is intended
    ///to contain all MemoryMappedArray Unit Tests
    ///</summary>
    [TestClass]
    public class MemoryMappedArray
    {
        private MemoryMappedArray<int> _testList;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion

        [TestInitialize]
        public void TestInit()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            _testList = new MemoryMappedArray<int>(10, path);
        }


        [TestMethod]
        public void Set_and_get_a_value_within_defined_range()
        {
            long position = (_testList.Length - 1000) >= 0 ? _testList.Length : 0;
            const int num = 234;
            _testList[position] = num;
            Assert.AreEqual(_testList[position], num);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Set_a_value_outside_defined_range_and_autogrow_is_false()
        {
            long position = _testList.Length + 1000;
            const int num = 234;
            _testList[position] = num;
        }

        [TestMethod]
        public void Set_and_get_a_value_outside_defined_range_and_autogrow_is_true()
        {
            _testList.AutoGrow = true;
            long position = _testList.Length + 1000;
            const int num = 234;
            _testList[position] = num;
            Assert.AreEqual(_testList[position], num);
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Set_a_negative_index_value()
        {
            _testList[-1] = 1;
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void Access_a_negative_index_value()
        {
            int test = _testList[-1];
        }

        [TestMethod]
        public void Iterate_over_all_values()
        {
            for (int i = 0; i < _testList.Length; i++)
            {
                _testList[i] = i;
            }

            int expected = 0;
            foreach (int i in _testList)
            {
                Assert.AreEqual(i, expected);
                expected++;
            }
        }
    }
}