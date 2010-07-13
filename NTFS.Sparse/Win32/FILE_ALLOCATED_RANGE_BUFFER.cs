using System;
using System.Collections.Generic;
using System.Text;

namespace NTFS.Sparse.Win32
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct FILE_ALLOCATED_RANGE_BUFFER
    {
        public Int64 FileOffset;
        public Int64 Length;
    }
}
