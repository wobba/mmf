using System;
using System.Diagnostics;
using System.Threading;
using mAdcOW.DataStructures;
using mAdcOW.DataStructures.DictionaryBacking;

namespace BenchmarkConsoleApp
{
    internal class Program
    {
        private static int MaxCount = 1000000;

        public static int Main(string[] args)
        {
            ThreadPool.SetMaxThreads(10, 1000);

            TextWriterTraceListener tr1 = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(tr1);

            SingelThread_HashInMemory();
            SingelThread_HashOnDisk();
            Threaded_HashInMemory();
            Threaded_HashOnDisk();
            return 0;
        }

        private static void Threaded_HashOnDisk()
        {
            Console.WriteLine("Threaded_HashOnDisk");
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<string, string> backingFile = new BackingUnknownSize<string, string>(path, MaxCount);

            Dictionary<string, string> dict = new Dictionary<string, string>(backingFile);

            Console.WriteLine("Queuing {0} items to Thread Pool", MaxCount);
            Console.WriteLine("Queue to Thread Pool 0");
            System.Collections.Generic.List<WaitHandle> handles = new System.Collections.Generic.List<WaitHandle>();
            Stopwatch sw = Stopwatch.StartNew();
            for (int iItem = 1; iItem < 20; iItem++)
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                handles.Add(mre);
                ThreadPool.QueueUserWorkItem(d =>
                                                 {
                                                     for (int i = 0; i < MaxCount/20; i++)
                                                     {
                                                         string key = Guid.NewGuid().ToString();
                                                         dict.Add(key, key);
                                                         if(string.IsNullOrEmpty(dict[key])) throw new Exception();
                                                     }
                                                     mre.Set();
                                                 }, null);
            }
            Console.WriteLine("Waiting for Thread Pool to drain");
            WaitHandle.WaitAll(handles.ToArray());
            sw.Stop();
            Console.WriteLine("Thread Pool has been drained (Event fired)");
            Console.WriteLine(sw.Elapsed);
        }

        private static void Threaded_HashInMemory()
        {
            Console.WriteLine("Threaded_HashInMemory");
            string path = AppDomain.CurrentDomain.BaseDirectory;
            DictionaryPersist<string, string> backingFile = new DictionaryPersist<string, string>(path, MaxCount);

            Dictionary<string, string> dict = new Dictionary<string, string>(backingFile);

            Console.WriteLine("Queuing {0} items to Thread Pool", MaxCount);
            Console.WriteLine("Queue to Thread Pool 0");
            System.Collections.Generic.List<WaitHandle> handles = new System.Collections.Generic.List<WaitHandle>();
            Stopwatch sw = Stopwatch.StartNew();
            for (int iItem = 1; iItem < 20; iItem++)
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                handles.Add(mre);
                ThreadPool.QueueUserWorkItem(d =>
                                                 {
                                                     for (int i = 0; i < MaxCount/20; i++)
                                                     {
                                                         string key = Guid.NewGuid().ToString();
                                                         dict.Add(key, key);
                                                         if (string.IsNullOrEmpty(dict[key])) throw new Exception();
                                                     }
                                                     mre.Set();
                                                 }, null);
            }
            Console.WriteLine("Waiting for Thread Pool to drain");
            WaitHandle.WaitAll(handles.ToArray());
            sw.Stop();
            Console.WriteLine("Thread Pool has been drained (Event fired)");
            Console.WriteLine(sw.Elapsed);
        }

        private static void SingelThread_HashOnDisk()
        {
            Console.WriteLine("SingelThread_HashOnDisk");
            string path = AppDomain.CurrentDomain.BaseDirectory;
            BackingUnknownSize<string, string> backingFile = new BackingUnknownSize<string, string>(path, MaxCount);

            Dictionary<string, string> dict = new Dictionary<string, string>(backingFile);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < MaxCount; i++)
            {
                string key = Guid.NewGuid().ToString();
                dict.Add(key, key);
                if (string.IsNullOrEmpty(dict[key])) throw new Exception();
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }

        private static void SingelThread_HashInMemory()
        {
            Console.WriteLine("SingelThread_HashInMemory");
            string path = AppDomain.CurrentDomain.BaseDirectory;
            DictionaryPersist<string, string> backingFile = new DictionaryPersist<string, string>(path, MaxCount);

            Dictionary<string, string> dict = new Dictionary<string, string>(backingFile);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < MaxCount; i++)
            {
                string key = Guid.NewGuid().ToString();
                dict.Add(key, key);
                if (string.IsNullOrEmpty(dict[key])) throw new Exception();
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }
    }
}