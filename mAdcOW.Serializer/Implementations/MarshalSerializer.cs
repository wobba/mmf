using System;
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

        private readonly byte[] _byteBuffer;
        private readonly bool _canSerialize;
        private readonly int _dataSize;
        private readonly IntPtr _unmanagedBufferPtr;

        #endregion

        public MarshalSerializer()
        {
            try
            {
                _dataSize = Marshal.SizeOf(typeof(T));
                _unmanagedBufferPtr = Marshal.AllocHGlobal(_dataSize);
                _byteBuffer = new byte[_dataSize];
                _canSerialize = true;
            }
            catch (Exception)
            {
                _canSerialize = false;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Marshal.DestroyStructure(_unmanagedBufferPtr, typeof(T));
            Marshal.FreeHGlobal(_unmanagedBufferPtr);
        }

        #endregion

        #region ISerializeDeserialize<T> Members

        public byte[] ObjectToBytes(T data)
        {
            Marshal.StructureToPtr(data, _unmanagedBufferPtr, true);
            byte[] buffer = new byte[_dataSize];
            Marshal.Copy(_unmanagedBufferPtr, buffer, 0, _dataSize);
            return buffer;
        }

        public T BytesToObject(byte[] bytes)
        {
            Marshal.Copy(bytes, 0, _unmanagedBufferPtr, _dataSize);
            object obj = Marshal.PtrToStructure(_unmanagedBufferPtr, typeof(T));
            return (T)obj;
        }

        public bool CanSerializeType()
        {
            return _canSerialize;
        }

        #endregion
    }
}