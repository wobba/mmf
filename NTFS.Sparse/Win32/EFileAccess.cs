using System;
using System.Collections.Generic;
using System.Text;

namespace NTFS.Sparse.Win32
{
    [Flags]
    public enum EFileAccess : uint
    {
        /// <summary>
        /// 
        /// </summary>
        GenericRead = 0x80000000,
        /// <summary>
        /// 
        /// </summary>
        GenericWrite = 0x40000000,
        /// <summary>
        /// 
        /// </summary>
        GenericExecute = 0x20000000,
        /// <summary>
        /// 
        /// </summary>
        GenericAll = 0x10000000
    }
}
