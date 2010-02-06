using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mAdcOW.Serializer
{
    /// <summary>
    /// Serializer implemented with the System.Runtime.InteropServices.Marshal namespace
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MarshalSerializer<T> : ISerializeDeserialize<T>, IDisposable
    {
        #region members

        private bool _isDisposed = false;
        private readonly bool _canSerialize;
        private readonly int _dataSize;
        static List<IntPtr> _unmanagedBufferPtrList = new List<IntPtr>();
        [ThreadStatic]
        private static IntPtr _unmanagedBufferPtr;

        #endregion

        public MarshalSerializer()
        {
            try
            {
                _dataSize = Marshal.SizeOf(typeof(T));
                _canSerialize = true;
            }
            catch (Exception)
            {
                _canSerialize = false;
            }
        }

        ~MarshalSerializer()
        {
            Dispose();
        }
        #region IDisposable Members

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            for (int i = 0; i < _unmanagedBufferPtrList.Count; i++)
            {
                if (_unmanagedBufferPtrList[i] == IntPtr.Zero) continue;

                Marshal.DestroyStructure(_unmanagedBufferPtrList[i], typeof (T));
                Marshal.FreeHGlobal(_unmanagedBufferPtrList[i]);
                _unmanagedBufferPtrList[i] = IntPtr.Zero;
            }
        }

        #endregion

        #region ISerializeDeserialize<T> Members

        public byte[] ObjectToBytes(T data)
        {
            EnsureBufferIsAvailable();
            Marshal.StructureToPtr(data, _unmanagedBufferPtr, true);
            byte[] buffer = new byte[_dataSize];
            Marshal.Copy(_unmanagedBufferPtr, buffer, 0, _dataSize);
            return buffer;
        }

        public T BytesToObject(byte[] bytes)
        {
            EnsureBufferIsAvailable();
            Marshal.Copy(bytes, 0, _unmanagedBufferPtr, _dataSize);
            object obj = Marshal.PtrToStructure(_unmanagedBufferPtr, typeof(T));
            return (T)obj;
        }

        private void EnsureBufferIsAvailable()
        {
            if (_unmanagedBufferPtr == IntPtr.Zero)
            {
                _unmanagedBufferPtr = Marshal.AllocHGlobal(_dataSize);
                _unmanagedBufferPtrList.Add(_unmanagedBufferPtr);
            }
        }

        public bool CanSerializeType()
        {
            return _canSerialize;
        }

        #endregion
    }
}