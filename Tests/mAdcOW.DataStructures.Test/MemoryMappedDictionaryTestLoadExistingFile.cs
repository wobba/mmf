using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.DataStructures.Test
{
    /// <summary>
    /// Summary description for MemoryMappedDictionaryTest
    /// </summary>
    [TestClass]
    public class MemoryMappedDictionaryTestLoadExistingFile
    {
        public TestContext TestContext { get; set; }
        string _path = AppDomain.CurrentDomain.BaseDirectory;
        private bool _error;
        private string _errorMessage;

        [TestMethod]
        public void When_opening_an_existing_file_validate_the_content()
        {
            Thread t = new Thread(CreateDict);
            t.Start();
            t.Join();
            Assert.IsFalse(_error, _errorMessage);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Thread t2 = new Thread(LoadExistingDict);
            t2.Start();
            t2.Join();
            Assert.IsFalse(_error, _errorMessage);

            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(4000);
            
            foreach (var file in Directory.GetFiles(_path, "test1.*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                }
            }
        }

        public void CreateDict()
        {
            try
            {
                var dict = new Dictionary<int, int>(_path, 20, true, "test1");
                dict[0] = 0;
                dict[1] = 1;
                Assert.AreEqual(2, dict.Count);
                Assert.AreEqual(0, dict[0]);
                Assert.AreEqual(1, dict[1]);
            }
            catch (AssertFailedException e)
            {
                _errorMessage = e.Message;
                _error = true;
            }
        }

        public void LoadExistingDict()
        {
            try
            {
                var dict = new Dictionary<int, int>(_path, 20, true, "test1");
                Assert.AreEqual(2, dict.Count);
                Assert.AreEqual(0, dict[0]);
                Assert.AreEqual(1, dict[1]);
            }
            catch (AssertFailedException e)
            {
                _errorMessage = e.Message;
                _error = true;
            }
        }
    }
}