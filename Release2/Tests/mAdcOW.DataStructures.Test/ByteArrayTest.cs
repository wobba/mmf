using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using mAdcOW.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataStructuresTest
{
    /// <summary>
    /// Summary description for ByteArrayTest
    /// </summary>
    [TestClass]
    public class ByteArrayTest
    {
        [TestMethod]
        public void When_passing_in_two_equal_arrays_verify_they_are_the_same_unsafe()
        {
            byte[] arr1 = new byte[] {1, 2, 3, 4, 5};
            byte[] arr2 = new byte[] {1, 2, 3, 4, 5};
            Assert.IsTrue(ByteArrayCompare.UnSafeEquals(arr1, arr2));
        }

        [TestMethod]
        public void When_passing_in_two_equal_arrays_verify_they_are_the_same_safe()
        {
            byte[] arr1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] arr2 = new byte[] { 1, 2, 3, 4, 5 };
            Assert.IsTrue(ByteArrayCompare.Equals(arr1, arr2));
        }

        [TestMethod]
        public void When_passing_in_two_different_arrays_verify_they_are_differen_unsafe()
        {
            byte[] arr1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] arr2 = new byte[] { 1, 2, 3, 4, 6 };
            Assert.IsFalse(ByteArrayCompare.UnSafeEquals(arr1, arr2));
        }

        [TestMethod]
        public void When_passing_in_two_different_arrays_verify_they_are_different_safe()
        {
            byte[] arr1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] arr2 = new byte[] { 1, 2, 3, 4, 6 };
            Assert.IsFalse(ByteArrayCompare.Equals(arr1, arr2));
        }
    }
}
