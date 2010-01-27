﻿using System;
using System.Collections.Generic;
using mAdcOW.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.DataStructures.Test
{
    /// <summary>
    /// Summary description for MemoryMappedDictionaryTest
    /// </summary>
    [TestClass]
    public class MemoryMappedDictionaryTestPrimitiveKeyValue
    {
        public TestContext TestContext { get; set; }

        private static mAdcOW.DataStructures.Dictionary<int, int> _dict;

        [ClassInitialize()]
        public static void InitializeDictionary(TestContext testContext)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            _dict = new mAdcOW.DataStructures.Dictionary<int, int>(path);
        }

        [TestInitialize()]
        public void ClearDictionary()
        {
            _dict.Clear();
        }

        [TestMethod]
        public void When_adding_an_item_verify_that_the_item_can_be_retreived()
        {
            _dict[0] = 0;
            Assert.AreEqual(0, _dict[0]);
        }

        [TestMethod]
        public void When_adding_an_item_verify_that_the_item_count()
        {
            TestClass t = new TestClass(10);
            _dict[0] = 0;
            Assert.AreEqual(1, _dict.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void When_retrieving_a_non_existing_key_throw_KeyNotFoundException()
        {
            int a = _dict[0];
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void When_deleting_a_key_thrown_KeyNotFoundException_when_accessing_it_afterwards()
        {
            _dict.Add(345, 345);
            bool removed =_dict.Remove(345);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _dict.Count);
            int a = _dict[345];
        }

        [TestMethod]
        public void When_checking_if_an_existing_key_exists_return_true()
        {
            _dict.Add(11, 11);
            Assert.IsTrue(_dict.ContainsKey(11));
        }

        [TestMethod]
        public void When_checking_if_a_nonexisting_key_exists_return_false()
        {
            _dict.Add(1, 1);
            Assert.IsFalse(_dict.ContainsKey(10));
        }

        [TestMethod]
        public void When_checking_if_an_existing_value_exists_return_true()
        {
            _dict.Add(11, 11);
            Assert.IsTrue(_dict.ContainsValue(11));
        }

        [TestMethod]
        public void When_checking_if_a_nonexisting_value_exists_return_false()
        {
            _dict.Add(1, 1);
            Assert.IsFalse(_dict.ContainsValue(10));
        }

        [TestMethod]
        public void When_trying_to_get_an_existing_value_return_the_value()
        {
            _dict.Add(1, 1);
            int actual;
            _dict.TryGetValue(1, out actual);
            Assert.AreEqual(1, actual);
        }

        [TestMethod]
        public void When_overwriting_an_existing_value_return_the_correct_value()
        {
            _dict[1] = 1;
            _dict[1] = 2;
            int actual;
            _dict.TryGetValue(1, out actual);
            Assert.AreEqual(2, actual);
        }

        [TestMethod]
        public void When_using_copyto_verify_the_result()
        {
            _dict[12] = 12;
            _dict[15] = 15;
            KeyValuePair<int, int>[] array = new KeyValuePair<int, int>[4];
            _dict.CopyTo(array, 2);
            Assert.AreEqual(12, array[2].Key);
            Assert.AreEqual(12, array[2].Value);

            Assert.AreEqual(15, array[3].Key);
            Assert.AreEqual(15, array[3].Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void When_copyto_a_null_array_throw_exception()
        {
            _dict[12] = 12;
            _dict[15] = 15;
            KeyValuePair<int, int>[] array = null;
            _dict.CopyTo(array, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void When_copyto_and_index_is_outside_array_size_throw_exception()
        {
            _dict[12] = 12;
            _dict[15] = 15;
            KeyValuePair<int, int>[] array = new KeyValuePair<int, int>[10];
            _dict.CopyTo(array, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void When_copyto_and_index_is_negative_throw_exception()
        {
            _dict[12] = 12;
            _dict[15] = 15;
            KeyValuePair<int, int>[] array = new KeyValuePair<int, int>[10];
            _dict.CopyTo(array, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void When_copyto_and_array_is_smaller_than_dictionary_throw_exception()
        {
            _dict[12] = 12;
            _dict[15] = 15;
            KeyValuePair<int, int>[] array = new KeyValuePair<int, int>[1];
            _dict.CopyTo(array, 0);
        }

        [TestMethod]
        public void When_adding_several_known_sized_items_verify_that_they_exist()
        {
            for (int i = 0; i < 5000; i++)
            {
                _dict[i] = i + 200;
            }

            for (int i = 0; i < 5000; i++)
            {
                Assert.AreEqual(i + 200, _dict[i]);
            }
        }
    }
}
