using System;
using System.Collections.Generic;
using System.Text;

namespace NTFS.Sparse.Win32
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct FILE_ZERO_DATA_INFORMATION
    {
        public Int64 FileOffset;
        public Int64 BeyondFinalZero;
    }
}