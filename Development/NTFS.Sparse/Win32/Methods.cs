using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace NTFS.Sparse.Win32
{
    internal class Methods
    {
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIoControlCode dwIoControlCode,
            IntPtr InBuffer,
            int nInBufferSize,
            IntPtr OutBuffer,
            int nOutBufferSize,
            ref int pBytesReturned,
            [In] ref NativeOverlapped lpOverlapped
            );

        /// Return Type: HANDLE->void*
        ///lpFileName: LPCWSTR->WCHAR*
        ///dwDesiredAccess: DWORD->unsigned int
        ///dwShareMode: DWORD->unsigned int
        ///lpSecurityAttributes: LPSECURITY_ATTRIBUTES->_SECURITY_ATTRIBUTES*
        ///dwCreationDisposition: DWORD->unsigned int
        ///dwFlagsAndAttributes: DWORD->unsigned int
        ///hTemplateFile: HANDLE->void*
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW")]
        public static extern SafeFileHandle CreateFileW(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            [In] IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            [In] IntPtr hTemplateFile);

        /// <summary>
        /// Get Information about a File System Volume, aka drive or partition.
        /// </summary>
        /// <param name="lpRootPathName">For example, @"C:\"</param>
        /// <param name="lpVolumeNameBuffer">StringBuilder object instantiated with non-zero capacity</param>
        /// <param name="nVolumeNameSize">Capacity of the StringBuilder passed into lpVolumeNameBuffer</param>
        /// <param name="lpVolumeSerialNumber">Serial number of the logical drive</param>
        /// <param name="lpMaximumComponentLength">Maximum length of a filename, typically 255 on NTFS v5.0</param>
        /// <param name="lpFileSystemFlags">Bit flags to indicate the features available on the logical drive aka volume</param>
        /// <param name="lpFileSystemNameBuffer">StringBuilder object instantiated with non-zero capacity</param>
        /// <param name="nFileSystemNameSize">Capacity of the StringBuilder passed into lpFileSystemNameBuffer</param>
        /// <returns>if false, ask for exception information via Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());</returns>
        [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVolumeInformationW(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpVolumeNameBuffer,
            uint nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpFileSystemNameBuffer,
            uint nFileSystemNameSize);
    }
}