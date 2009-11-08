using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Winterdom.IO.FileMap;
using Timer = System.Timers.Timer;

namespace mAdcOW.DataStructures
{
    class ViewManager : IViewManager
    {
        private readonly System.Collections.Generic.Dictionary<int, MapViewStream> _viewThreadPool = new System.Collections.Generic.Dictionary<int, MapViewStream>(10);
        private readonly System.Collections.Generic.Dictionary<int, DateTime> _lastUsedThread = new System.Collections.Generic.Dictionary<int, DateTime>();
        private readonly ReaderWriterLockSlim _viewLock = new ReaderWriterLockSlim();
        private MemoryMappedFile _map;
        private bool _deleteFile = true;
        private string _fileName;
        private long _fileSize;
        private int _dataSize;
        private const int GrowPercentage = 20;

        private Timer _pooltimer;

        public ViewManager()
        {
            InitializeThreadPoolCleanUpTimer();
        }

        ~ViewManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Get a working view for the current thread
        /// </summary>
        /// <param name="threadId"></param>
        /// <returns></returns>
        public Stream GetView(int threadId)
        {
            _viewLock.EnterUpgradeableReadLock();
            try
            {
                _lastUsedThread[threadId] = DateTime.UtcNow;
                MapViewStream s;
                if (_viewThreadPool.TryGetValue(threadId, out s))
                {
                    return s;
                }
                return AddNewViewToThreadPool(threadId);
            }
            finally
            {
                _viewLock.ExitUpgradeableReadLock();
            }
        }

        public void Initialize(string fileName, long capacity, int dataSize)
        {
            _dataSize = dataSize;
            _fileSize = capacity * dataSize;
            _fileName = fileName;
            _map = MemoryMappedFile.Create(fileName, MapProtection.PageReadWrite, _fileSize);
        }

        public long Length
        {
            get
            {
                return _fileSize / _dataSize;
            }
        }

        private Stream AddNewViewToThreadPool(int threadId)
        {
            _viewLock.EnterWriteLock();
            try
            {
                MapViewStream mvs;
                _viewThreadPool[threadId] = mvs = _map.MapAsStream();
                return mvs;
            }
            finally
            {
                _viewLock.ExitWriteLock();
            }
        }

        private void EnsureBackingFile()
        {
            if (_map == null || !_map.IsOpen)
            {
                _map = MemoryMappedFile.Create(_fileName, MapProtection.PageReadWrite, _fileSize);
            }
        }

        private void InitializeThreadPoolCleanUpTimer()
        {
            _pooltimer = new Timer();
            _pooltimer.Elapsed += DisposeAndRemoveUnusedViews;
            _pooltimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
            _pooltimer.AutoReset = true;
            _pooltimer.Start();
        }

        /// <summary>
        /// Clean up unused views
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DisposeAndRemoveUnusedViews(object sender, System.Timers.ElapsedEventArgs e)
        {
            _viewLock.EnterWriteLock();
            try
            {
                foreach (int threadId in FindThreadsToClean(-1))
                {
                    CleanThreadPool(threadId);
                }
            }
            finally
            {
                _viewLock.ExitWriteLock();
            }
        }

        private System.Collections.Generic.List<int> FindThreadsToClean(int hours)
        {
            System.Collections.Generic.List<int> cleanedThreads = new System.Collections.Generic.List<int>(_lastUsedThread.Count);
            foreach (KeyValuePair<int, DateTime> pair in _lastUsedThread)
            {
                if (pair.Value < DateTime.UtcNow.AddHours(hours))
                {
                    cleanedThreads.Add(pair.Key);
                }
            }
            return cleanedThreads;
        }

        private void CleanThreadPool(int threadId)
        {
            if (_viewThreadPool.ContainsKey(threadId))
            {
                _viewThreadPool[threadId].Close();
                _viewThreadPool.Remove(threadId);
            }
            _lastUsedThread.Remove(threadId);
        }

        public bool EnoughBackingCapacity(long position, long writeLength)
        {
            return (position + writeLength) <= _fileSize;
        }

        /// <summary>
        /// Grow the array to support more data
        /// </summary>
        /// <param name="sizeToGrowFrom">The size to grow from</param>
        public void Grow(long sizeToGrowFrom)
        {
            Grow(sizeToGrowFrom, GrowPercentage);
        }

        public void CleanUp()
        {
            Dispose();
        }

        /// <summary>
        /// Grow the array to support more data
        /// </summary>
        /// <param name="size">The size to grow from</param>
        /// <param name="percentage">The percentage to grow with</param>
        private void Grow(long size, int percentage)
        {
            _viewLock.EnterWriteLock();
            try
            {
                _deleteFile = false; // don't delete the file, only grow                
                SetNewFileSize(size, percentage);
                Dispose(true); // Clean up before growing the file
                EnsureBackingFile();
                _deleteFile = true; // reset deletefile flag
            }
            finally
            {
                _viewLock.ExitWriteLock();
            }
        }

        private void SetNewFileSize(long size, int percentage)
        {
            long oldSize = _fileSize;
            long newSize = oldSize + _dataSize;
            _fileSize = (long)((float)size * _dataSize * ((100F + percentage) / 100F)); //required filesize
            if (_fileSize < newSize)
            {
                _fileSize = newSize;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing || _deleteFile)
            {
                DisposeAllViews();
                CloseMapFile();
            }
            CleanUpBackingFile();
        }

        private void CloseMapFile()
        {
            if (_map != null)
            {
                _map.Close();
            }
        }

        private void CleanUpBackingFile()
        {
            try
            {
                if (_deleteFile)
                {
                    if (File.Exists(_fileName)) File.Delete(_fileName);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                // TODO: Handle files which for some reason didn't want to be deleted
                Trace.WriteLine(e.Message);
                //throw;
            }
        }

        private void DisposeAllViews()
        {
            System.Collections.Generic.List<int> cleanedThreads = new System.Collections.Generic.List<int>(_viewThreadPool.Count);
            foreach (var threadPoolEntry in _viewThreadPool)
            {
                cleanedThreads.Add(threadPoolEntry.Key);
            }
            foreach (int threadId in cleanedThreads)
            {
                CleanThreadPool(threadId);
            }
        }
    }
}