using System;
using System.Collections.Generic;
using System.Threading;
using mAdcOW.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataStructuresTest
{
    [TestClass]
    public class ThreadingTest
    {
        #region test1
        [TestMethod]
        public void Thread_load_test()
        {
            _error = false;
            string path = AppDomain.CurrentDomain.BaseDirectory;
            using (Array<int> testList = new Array<int>(10, path))
            {
                testList.AutoGrow = true;
                System.Collections.Generic.List<Thread> tList = new System.Collections.Generic.List<Thread>(100);
                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoWrite);
                    tList.Add(t);
                    t.Start(testList);
                }

                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoRead);
                    tList.Add(t);
                    t.Start(testList);
                }

                foreach (Thread t in tList)
                {
                    t.Join();
                }
                Assert.IsFalse(_error);
            }
        }

        private static bool _error;
        private static void DoWrite(object list)
        {
            try
            {
                Random random = new Random();
                Array<int> intList = (Array<int>)list;
                for (int i = 0; i < 100000; i++)
                {
                    intList[random.Next(1000)] = i;
                }
            }
            catch (Exception)
            {
                _error = true;
                throw;
            }
        }

        private static void DoRead(object list)
        {
            try
            {
                Random random = new Random();
                Array<int> intList = (Array<int>)list;
                for (int i = 0; i < 100000; i++)
                {
                    int x = intList[random.Next(1000)];
                }
            }
            catch (Exception)
            {
                _error = true;
                throw;
            }
        }
        #endregion
    }
}
