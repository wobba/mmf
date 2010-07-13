using System;
using System.Collections.Generic;
using System.Text;

namespace NTFS.Sparse.Win32
{
    [Flags]
    public enum EIoMethod : uint
    {
        Buffered = 0,
        InDirect = 1,
        OutDirect = 2,
        Neither = 3
    }
}
