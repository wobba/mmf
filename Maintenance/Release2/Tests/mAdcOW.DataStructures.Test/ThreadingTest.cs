using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataStructuresTest
{
    [TestClass]
    public class ThreadingTest
    {
        #region test1
        [TestMethod]
        public void Array_thread_test()
        {
            _error = false;
            string path = AppDomain.CurrentDomain.BaseDirectory;
            using (mAdcOW.DataStructures.Array<int> testList = new mAdcOW.DataStructures.Array<int>(10, path))
            {
                testList.AutoGrow = true;
                List<Thread> tList = new List<Thread>(100);
                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoWriteTest1);
                    tList.Add(t);
                    t.Start(testList);
                }

                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoReadTest1);
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
        private static void DoWriteTest1(object list)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.Array<int> intList = (mAdcOW.DataStructures.Array<int>)list;
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

        private static void DoReadTest1(object list)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.Array<int> intList = (mAdcOW.DataStructures.Array<int>)list;
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

        #region test2
        [TestMethod]
        public void List_thread_test()
        {
            _error = false;
            string path = AppDomain.CurrentDomain.BaseDirectory;
            using (mAdcOW.DataStructures.List<int> testList = new mAdcOW.DataStructures.List<int>(10, path))
            {
                List<Thread> tList = new List<Thread>(100);
                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoWriteTest2);
                    tList.Add(t);
                    t.Start(testList);
                }

                for (int i = 0; i < 100; i++)
                {
                    Thread t = new Thread(DoReadTest2);
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

        private static void DoWriteTest2(object list)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.List<int> intList = (mAdcOW.DataStructures.List<int>)list;
                for (int i = 0; i < 100000; i++)
                {
                    int pos = random.Next(1000);
                    if (pos >= intList.Count)
                    {
                        intList.Add(random.Next(1000));
                    }
                    else
                    {
                        intList[pos] = i;
                    }
                }
            }
            catch (Exception)
            {
                _error = true;
                throw;
            }
        }

        private static void DoReadTest2(object list)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.List<int> intList = (mAdcOW.DataStructures.List<int>)list;
                for (int i = 0; i < 100000; i++)
                {
                    int pos = random.Next(1000);
                    if (pos >= intList.Count)
                    {
                        intList.Add(random.Next(1000));
                    }
                    else
                    {
                        int x = intList[pos];
                    }
                }
            }
            catch (Exception)
            {
                _error = true;
                throw;
            }
        }
        #endregion

        #region test3
        [TestMethod]
        public void Dictionary_thread_test()
        {
            _error = false;
            string path = AppDomain.CurrentDomain.BaseDirectory;
            mAdcOW.DataStructures.Dictionary<int, int> testDictionary = new mAdcOW.DataStructures.Dictionary<int, int>(path);

            List<Thread> tList = new List<Thread>(100);
            for (int i = 0; i < 20; i++)
            {
                Thread t = new Thread(DoWriteTest3);
                tList.Add(t);
                t.Start(testDictionary);
            }

            for (int i = 0; i < 20; i++)
            {
                Thread t = new Thread(DoReadTest3);
                tList.Add(t);
                t.Start(testDictionary);
            }

            foreach (Thread t in tList)
            {
                t.Join();
            }
            Assert.IsFalse(_error);
        }

        private static void DoWriteTest3(object dictionary)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.Dictionary<int, int> dictionary1 = (mAdcOW.DataStructures.Dictionary<int, int>)dictionary;
                for (int i = 0; i < 100000; i++)
                {
                    dictionary1[random.Next(1000)] = i;
                }
            }
            catch (Exception)
            {
                _error = true;
                throw;
            }
        }

        private static void DoReadTest3(object dictionary)
        {
            try
            {
                Random random = new Random();
                mAdcOW.DataStructures.Dictionary<int, int> dictionary1 = (mAdcOW.DataStructures.Dictionary<int, int>)dictionary;
                for (int i = 0; i < 100000; i++)
                {
                    int x;
                    dictionary1.TryGetValue(random.Next(1000), out x);
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
