//
//
// Array.cs
//    
//    Implementation of a generic valuetype array using Win32 Memory Mapped
//    Files for storage.
//
// COPYRIGHT (C) 2009, Mikael Svenson (miksvenson@gmail.com)
//    Second implementation
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using mAdcOW.Serializer;

namespace mAdcOW.DataStructures
{
    /// <summary>
    /// Array represent an array and stores it on disk using Memory Mapped Files
    /// instead of keeping the data in process memory. Memory Mapped Files will use the OS'
    /// functions for optimal caching of the data, yielding a reasonable tradeoff between
    /// speed and large amounts of data.
    /// 
    /// .Net applications will typically give random out-of-memory exceptions when approaching
    /// ~800mb data structures, specially if you need to keep several copies of an instance at
    /// a time. The problem is less frequent on 64bit systems than on 32bit, but still there.
    /// 
    /// This class will only accept value types and structs (which is a value type) since those
    /// objects always will take up the same amount of space. But make sure the struct contains
    /// only value types, or defined length strings.
    /// </summary>
    public class Array<T> : IEnumerable<T>, IDisposable
        where T : struct
    {
        #region Private fields
        private int _dataSize;
        private readonly ReaderWriterLockSlim _valueLock = new ReaderWriterLockSlim();
        protected ISerializeDeserialize<T> ValueSerializer;
        protected readonly IViewManager ViewManager;
        #endregion

        #region Properties
        /// <summary>
        /// The unique name of the file stored on disk
        /// </summary>
        private string UniqueName { get; set; }

        /// <summary>
        /// Allow array to automatically grow if you access an indexer larger than the starting size
        /// </summary>
        public bool AutoGrow { get; set; }

        /// <summary>
        /// Return the number of elements in the array
        /// </summary>
        public virtual long Length
        {
            get { return ViewManager.Length; }
        }

        private long Capacity
        {
            get { return Length; }
        }

        /// <summary>
        /// Set the position before setting or getting data
        /// </summary>
        internal long Position
        {
            set
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Stream s = ViewManager.GetView(threadId);
                s.Position = value * _dataSize;
            }
        }

        public override string ToString()
        {
            return string.Format("Length {0}", Length);
        }

        public void Dispose()
        {
            if (ViewManager != null)
            {
                ViewManager.CleanUp();
            }
        }

        #endregion

        #region Constructor
        /// <summary>
        /// Create a new memory mapped array on disk
        /// </summary>
        /// <param name="capacity">The length of the array to allocate</param>
        /// <param name="path">The directory where the memory mapped file is to be stored</param>
        public Array(long capacity, string path)
            : this(capacity, path, false)
        {
        }

        /// <summary>
        /// Create a new memory mapped array on disk
        /// </summary>
        /// <param name="capacity">The number of elements to allocate in the array</param>
        /// <param name="path">The directory where the memory mapped file is to be stored</param>
        /// <param name="autoGrow">Decide if the array can expand or not</param>
        public Array(long capacity, string path, bool autoGrow)
            : this(capacity, path, autoGrow, new Factory<T>().GetSerializer(), new ViewManager())
        {
        }

        public Array(long capacity, string path, bool autoGrow, ISerializeDeserialize<T> serializer, IViewManager viewManager)
        {
            ValueSerializer = serializer;
            ViewManager = viewManager;

            UniqueName = "mmf-" + Guid.NewGuid();
            AutoGrow = autoGrow;

            InitWorkerBuffers();
            string fileName = Path.Combine(path, UniqueName + ".bin");
            ViewManager.Initialize(fileName, capacity, _dataSize);
        }

        private void InitWorkerBuffers()
        {
            _dataSize = Marshal.SizeOf(typeof(T));
        }

        ~Array()
        {
            Dispose();
        }

        #endregion

        internal void Write(byte[] buffer, long index)
        {
            Stream viewStream = GetThreadStream();
            viewStream.Position = index * _dataSize;
            WriteBufferToStream(viewStream, buffer);
        }

        internal void Write(byte[] buffer)
        {
            Stream viewStream = GetThreadStream();
            WriteBufferToStream(viewStream, buffer);
        }

        private Stream GetThreadStream()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            return ViewManager.GetView(threadId);
        }

        private void WriteBufferToStream(Stream viewStream, byte[] buffer)
        {
            if (NeedToGrowView(viewStream.Position, buffer))
            {
                viewStream = GrowViewAndGetNewStream(viewStream.Position, buffer);
            }
            viewStream.Write(buffer, 0, buffer.Length);
        }

        private bool NeedToGrowView(long streamPosition, byte[] buffer)
        {
            return AutoGrow && !ViewManager.EnoughBackingCapacity(streamPosition, buffer.LongLength);
        }

        private Stream GrowViewAndGetNewStream(long originalPosition, byte[] buffer)
        {
            ViewManager.Grow(originalPosition + buffer.LongLength);
            Stream viewStream = GetThreadStream();
            viewStream.Position = originalPosition;
            return viewStream;
        }

        /// <summary>
        /// Reads a T from the current position
        /// </summary>
        /// <returns>Byte array of the size of T</returns>
        internal byte[] Read()
        {
            return MultiRead(_dataSize);
        }

        internal byte[] Read(long index)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Stream s = ViewManager.GetView(threadId);
            s.Position = index * _dataSize;

            byte[] buffer = new byte[_dataSize];
            return FillBufferFromStream(buffer, s);
        }

        internal byte[] MultiRead(int count)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            Stream s = ViewManager.GetView(threadId);
            byte[] buffer = new byte[count];
            return FillBufferFromStream(buffer, s);
        }

        private byte[] FillBufferFromStream(byte[] buffer, Stream stream)
        {
            int bytesRead = 0;
            do
            {
                bytesRead = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
            } while (bytesRead != _dataSize && bytesRead > 0);
            return buffer;
        }

        internal byte ReadByte()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Stream s = ViewManager.GetView(threadId);
            return (byte)s.ReadByte();
        }

        public T this[long index]
        {
            get
            {
                if (index >= Length || index < 0)
                {
                    throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                }

                _valueLock.EnterReadLock();
                try
                {
                    return ValueSerializer.BytesToObject(Read(index));
                }
                finally
                {
                    _valueLock.ExitReadLock();
                }
            }
            set
            {
                if (index < 0)
                {
                    throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                }
                _valueLock.EnterWriteLock();
                try
                {
                    if (index >= Capacity)
                    {
                        if (AutoGrow)
                        {
                            ViewManager.Grow(index);
                        }
                        else
                        {
                            throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                        }
                    }
                    Write(ValueSerializer.ObjectToBytes(value), index);
                }
                finally
                {
                    _valueLock.ExitWriteLock();
                }
            }
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            _valueLock.EnterReadLock();
            try
            {
                Position = 0;
                for (int i = 0; i < Length; i++)
                {
                    yield return ValueSerializer.BytesToObject(Read());
                }
            }
            finally
            {
                _valueLock.ExitReadLock();
            }
        }

        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}