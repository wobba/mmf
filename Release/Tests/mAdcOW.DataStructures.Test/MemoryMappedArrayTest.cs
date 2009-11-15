using System;
using mAdcOW.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataStructuresTest
{
    /// <summary>
    ///This is a test class for MemoryMappedArrayTest and is intended
    ///to contain all MemoryMappedArrayTest Unit Tests
    ///</summary>
    [TestClass]
    public class MemoryMappedArrayTest
    {
        private Array<int> _testList;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void MyTestCleanup()
        {
            _testList.Dispose();
        }

        [TestInitialize]
        public void TestInit()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            _testList = new Array<int>(10, path);
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