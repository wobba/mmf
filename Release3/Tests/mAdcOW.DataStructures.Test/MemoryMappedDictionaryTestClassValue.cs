using System;
using System.Collections.Generic;
using mAdcOW.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.DataStructures.Test
{
    public class TestClass
    {
        public string Name { get; set; }

        public TestClass()
        {
        }

        public TestClass(int length)
        {
            Name = "".PadLeft(length, 'a');
        }

        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(Name)) return Name.GetHashCode();
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TestClass)) return false;
            return string.Equals(((TestClass)obj).Name, Name);
        }
    }

    /// <summary>
    /// Summary description for MemoryMappedDictionaryTest
    /// </summary>
    [TestClass]
    public class MemoryMappedDictionaryTestClassValue
    {
        public TestContext TestContext { get; set; }

        private static mAdcOW.DataStructures.Dictionary<int, TestClass> _dict;

        [ClassInitialize()]
        public static void InitializeDictionary(TestContext testContext)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            _dict = new mAdcOW.DataStructures.Dictionary<int, TestClass>(path);
        }

        [TestInitialize()]
        public void ClearDictionary()
        {
            _dict.Clear();
        }

        [TestMethod]
        public void When_adding_an_item_verify_that_the_item_can_be_retreived()
        {
            TestClass t = new TestClass(10);
            _dict[0] = t;
            Assert.AreEqual(t, _dict[0]);
        }

        [TestMethod]
        public void When_adding_an_item_verify_that_the_item_count()
        {
            TestClass t = new TestClass(10);
            _dict[0] = t;
            Assert.AreEqual(1, _dict.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void When_retrieving_a_non_existing_key_throw_KeyNotFoundException()
        {
            TestClass a = _dict[0];
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void When_deleting_a_key_thrown_KeyNotFoundException_when_accessing_it_afterwards()
        {
            TestClass t = new TestClass(10);
            _dict.Add(345, t);
            bool removed = _dict.Remove(345);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _dict.Count);
            t = _dict[345];
        }

        [TestMethod]
        public void When_checking_if_an_existing_key_exists_return_true()
        {
            TestClass t = new TestClass(10);
            _dict.Add(11, t);
            Assert.IsTrue(_dict.ContainsKey(11));
        }

        [TestMethod]
        public void When_checking_if_a_nonexisting_key_exists_return_false()
        {
            TestClass t = new TestClass(10);
            _dict.Add(1, t);
            Assert.IsFalse(_dict.ContainsKey(10));
        }

        [TestMethod]
        public void When_checking_if_an_existing_value_exists_return_true()
        {
            TestClass t = new TestClass(10);
            _dict.Add(11, t);
            Assert.IsTrue(_dict.ContainsValue(t));
        }

        [TestMethod]
        public void When_checking_if_a_nonexisting_value_exists_return_false()
        {
            TestClass t = new TestClass(10);
            _dict.Add(1, t);
            TestClass t2 = new TestClass(11);
            Assert.IsFalse(_dict.ContainsValue(t2));
        }

        [TestMethod]
        public void When_trying_to_get_an_existing_value_return_the_value()
        {
            TestClass t = new TestClass(10);
            _dict.Add(1, t);
            TestClass actual;
            _dict.TryGetValue(1, out actual);
            Assert.AreEqual(t, actual);
        }

        [TestMethod]
        public void When_overwriting_an_existing_value_return_the_correct_value()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);
            _dict[1] = t;
            _dict[1] = t2;
            TestClass actual;
            _dict.TryGetValue(1, out actual);
            Assert.AreEqual(t2, actual);
        }

        [TestMethod]
        public void When_using_copyto_verify_the_result()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);

            _dict[12] = t;
            _dict[15] = t2;
            KeyValuePair<int, TestClass>[] array = new KeyValuePair<int, TestClass>[4];
            _dict.CopyTo(array, 2);
            Assert.AreEqual(12, array[2].Key);
            Assert.AreEqual(t, array[2].Value);

            Assert.AreEqual(15, array[3].Key);
            Assert.AreEqual(t2, array[3].Value);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void When_copyto_a_null_array_throw_exception()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);
            _dict[12] = t;
            _dict[15] = t2;
            KeyValuePair<int, TestClass>[] array = null;
            _dict.CopyTo(array, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void When_copyto_and_index_is_outside_array_size_throw_exception()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);
            _dict[12] = t;
            _dict[15] = t2;
            KeyValuePair<int, TestClass>[] array = new KeyValuePair<int, TestClass>[10];
            _dict.CopyTo(array, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void When_copyto_and_index_is_negative_throw_exception()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);
            _dict[12] = t;
            _dict[15] = t2;
            KeyValuePair<int, TestClass>[] array = new KeyValuePair<int, TestClass>[10];
            _dict.CopyTo(array, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void When_copyto_and_array_is_smaller_than_dictionary_throw_exception()
        {
            TestClass t = new TestClass(10);
            TestClass t2 = new TestClass(11);
            _dict[12] = t;
            _dict[15] = t2;
            KeyValuePair<int, TestClass>[] array = new KeyValuePair<int, TestClass>[1];
            _dict.CopyTo(array, 0);
        }

        [TestMethod]
        public void When_adding_several_known_sized_items_verify_that_they_exist()
        {
            for (int i = 0; i < 100; i++)
            {
                _dict[i] = new TestClass(i + 20);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(new TestClass(i + 20), _dict[i]);
            }
        }

        [TestMethod]
        public void When_iteration_over_the_dictionary_throw_exception_if_items_are_removed()
        {
            _dict[0] = new TestClass(0);
            _dict[1] = new TestClass(1);
            var enumerator = _dict.GetEnumerator();
            enumerator.MoveNext();
            _dict.Remove(0);
            MyAssert.ThrowsException<InvalidOperationException>(() => { enumerator.MoveNext(); });
        }

        [TestMethod]
        public void When_iteration_over_the_dictionary_throw_exception_if_items_are_added_by_accessor()
        {
            _dict[0] = new TestClass(0);
            _dict[1] = new TestClass(1);
            var enumerator = _dict.GetEnumerator();
            enumerator.MoveNext();
            _dict[2] = new TestClass(2);
            MyAssert.ThrowsException<InvalidOperationException>(() => { enumerator.MoveNext(); });
        }

        [TestMethod]
        public void When_iteration_over_the_dictionary_throw_exception_if_items_are_added()
        {
            _dict[0] = new TestClass(0);
            _dict[1] = new TestClass(1);
            var enumerator = _dict.GetEnumerator();
            enumerator.MoveNext();
            _dict.Add(2, new TestClass(2));
            MyAssert.ThrowsException<InvalidOperationException>(() => { enumerator.MoveNext(); });
        }
    }
}
