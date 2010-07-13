//
// MemoryMappedFile.cs
//    
//    Implementation of a library to use Win32 Memory Mapped
//    Files from within .NET applications
//
// COPYRIGHT (C) 2001, Tomas Restrepo (tomasr@mvps.org)
//   Original concept and implementation
// COPYRIGHT (C) 2006, Steve Simpson (s.simpson64@gmail.com)
//   Modifications to *improve* coupling with MapViewStream
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
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Winterdom.IO.FileMap
{
    /// <summary>
    ///   Specifies page protection for the mapped file
    ///   These correspond to the PAGE_XXX set of flags
    ///   passed to CreateFileMapping()
    /// </summary>
    [Flags]
    public enum MapProtection
    {
        PageNone = 0x00000000,
        // protection - mutually exclusive, do not or
        PageReadOnly = 0x00000002,
        PageReadWrite = 0x00000004,
        PageWriteCopy = 0x00000008,
        // attributes - or-able with protection
        SecImage = 0x01000000,
        SecReserve = 0x04000000,
        SecCommit = 0x08000000,
        SecNoCache = 0x10000000,
    }

    /// <summary>
    ///   Specifies access for the mapped file.
    ///   These correspond to the FILE_MAP_XXX
    ///   constants used by MapViewOfFile[Ex]()
    /// </summary>
    public enum MapAccess
    {
        FileMapCopy = 0x0001,
        FileMapWrite = 0x0002,
        FileMapRead = 0x0004,
        FileMapAllAccess = 0x001f,
    }

    /// <summary>Wrapper class around the Win32 MMF APIs</summary>
    /// <remarks>
    ///    Allows you to easily use memory mapped files on
    ///    .NET applications.
    ///    Currently, not all functionality provided by 
    ///    the Win32 system is avaliable. Things that are not 
    ///    supported include:
    ///    <list>
    ///       <item>You can't specify security descriptors</item>
    ///       <item>You can't build the memory mapped file
    ///           on top of a System.IO.File already opened</item>
    ///    </list>
    ///    The class is currently MarshalByRefObject, but I would
    ///    be careful about possible interactions!
    /// </remarks>
    public class MemoryMappedFile : MarshalByRefObject, IDisposable
    {
        //! handle to MemoryMappedFile object
        private IntPtr _hMap = IntPtr.Zero;
        private MapProtection _protection = MapProtection.PageNone;

        private string _fileName = "";

        public string FileName
        {
            get { return _fileName; }
        }

        private long _maxSize;

        public long MaxSize
        {
            get { return _maxSize; }
        }

        #region Constants

        private const int GENERIC_READ = unchecked((int) 0x80000000);
        private const int GENERIC_WRITE = unchecked(0x40000000);
        private const int OPEN_ALWAYS = 4;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private static readonly IntPtr NULL_HANDLE = IntPtr.Zero;

        #endregion // Constants

        public bool IsOpen
        {
            get { return (_hMap != NULL_HANDLE); }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private MemoryMappedFile()
        {
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MemoryMappedFile()
        {
            Dispose(false);
        }

        #region Create Overloads

        /// <summary>
        ///   Create an unnamed map object with no file backing
        /// </summary>
        /// <param name="protection">desired access to the 
        ///            mapping object</param>
        /// <param name="maxSize">maximum size of filemap object</param>
        /// <param name="name">name of file mapping object</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile
            Create(MapProtection protection, long maxSize, string name)
        {
            return Create(null, protection, maxSize, name);
        }

        /// <summary>
        ///   Create an named map object with no file backing
        /// </summary>
        /// <param name="protection">desired access to the 
        ///            mapping object</param>
        /// <param name="maxSize">maximum size of filemap object</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile
            Create(MapProtection protection, long maxSize)
        {
            return Create(null, protection, maxSize, null);
        }

        /// <summary>
        ///   Create an unnamed map object with a maximum size
        ///   equal to that of the file
        /// </summary>
        /// <param name="fileName">name of backing file</param>
        /// <param name="protection">desired access to the 
        ///            mapping object</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile
            Create(string fileName, MapProtection protection)
        {
            return Create(fileName, protection, 0, null);
        }

        /// <summary>
        ///   Create an unnamed map object 
        /// </summary>
        /// <param name="fileName">name of backing file</param>
        /// <param name="protection">desired access to the 
        ///            mapping object</param>
        /// <param name="maxSize">maximum size of filemap 
        ///            object, or 0 for size of file</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile
            Create(string fileName, MapProtection protection,
                   long maxSize)
        {
            return Create(fileName, protection, maxSize, null);
        }

        /// <summary>
        ///   Create a named map object 
        /// </summary>
        /// <param name="fileName">name of backing file, or null 
        ///            for a pagefile-backed map</param>
        /// <param name="protection">desired access to the mapping 
        ///            object</param>
        /// <param name="maxSize">maximum size of filemap object, or 0 
        ///            for size of file</param>
        /// <param name="name">name of file mapping object</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile
            Create(string fileName, MapProtection protection,
                   long maxSize, String name)
        {
            MemoryMappedFile map = new MemoryMappedFile();

            // open file first
            IntPtr hFile = INVALID_HANDLE_VALUE;
            SafeFileHandle handle = null;

            if (!string.IsNullOrEmpty(fileName))
            {
                if (maxSize == 0)
                {
                    if (!File.Exists(fileName))
                    {
                        throw new Exception(
                            string.Format(
                                "Winterdom.IO.FileMap.MemoryMappedFile.Create - \"{0}\" does not exist ==> Unable to map entire file",
                                fileName));
                    }

                    FileInfo backingFileInfo = new FileInfo(fileName);
                    maxSize = backingFileInfo.Length;

                    if (maxSize == 0)
                    {
                        throw new Exception(
                            string.Format(
                                "Winterdom.IO.FileMap.MemoryMappedFile.Create - \"{0}\" is zero bytes ==> Unable to map entire file",
                                fileName));
                    }
                }

                // determine file access needed
                // we'll always need generic read access
                int desiredAccess = GENERIC_READ;
                if ((protection == MapProtection.PageReadWrite) ||
                    (protection == MapProtection.PageWriteCopy))
                {
                    desiredAccess |= GENERIC_WRITE;
                }

                // open or create the file
                // if it doesn't exist, it gets created
                //hFile = Win32MapApis.CreateFile(
                //    fileName, desiredAccess, 0,
                //    IntPtr.Zero, OPEN_ALWAYS, 0, IntPtr.Zero
                //    );
                FileStream fileStream = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite,FileShare.ReadWrite);
                handle = fileStream.SafeFileHandle;
                
                if( handle == null || handle.IsInvalid)
                //if (hFile == INVALID_HANDLE_VALUE)
                    throw new FileMapIOException(Marshal.GetHRForLastWin32Error());

                map._fileName = fileName;
            }

            map._hMap = Win32MapApis.CreateFileMapping(
                handle, IntPtr.Zero, (int) protection,
                (int) ((maxSize >> 32) & 0xFFFFFFFF),
                (int) (maxSize & 0xFFFFFFFF), name
                );

            // close file handle, we don't need it
            //if (hFile != INVALID_HANDLE_VALUE) Win32MapApis.CloseHandle(hFile);
            if (map._hMap == NULL_HANDLE)
                throw new FileMapIOException(Marshal.GetHRForLastWin32Error());

            map._protection = protection;
            map._maxSize = maxSize;

            return map;
        }

        #endregion // Create Overloads

        /// <summary>
        ///   Open an existing named File Mapping object
        /// </summary>
        /// <param name="access">desired access to the map</param>
        /// <param name="name">name of object</param>
        /// <returns>The memory mapped file instance</returns>
        public static MemoryMappedFile Open(MapAccess access, String name)
        {
            MemoryMappedFile map = new MemoryMappedFile();

            map._hMap = Win32MapApis.OpenFileMapping((int) access, false, name);
            if (map._hMap == NULL_HANDLE)
                throw new FileMapIOException(Marshal.GetHRForLastWin32Error());
            map._maxSize = -1; // sws debug unknown
            return map;
        }

        /// <summary>
        ///   Close this File Mapping object
        ///   From here on, You can't do anything with it
        ///   but the open views remain valid.
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        public IntPtr MapView(MapAccess access, long offset, long size)
        {
            if (!IsOpen)
                throw new ObjectDisposedException("Winterdom.IO.FileMap.MemoryMappedFile.MapView - MMF already closed");

            IntPtr baseAddress = IntPtr.Zero;
            IntPtr mapSize = new IntPtr(size);
                // sws note Throws OverflowException if (a) this is a 32-bit platform AND (b) size is out of bounds (ie. int bounds) with respect to this platform

            baseAddress = Win32MapApis.MapViewOfFile(
                _hMap, (int) access,
                (int) ((offset >> 32) & 0xFFFFFFFF),
                (int) (offset & 0xFFFFFFFF), mapSize
                );

            if (baseAddress == IntPtr.Zero)
                throw new FileMapIOException(Marshal.GetHRForLastWin32Error());

            return baseAddress;
        }

        /// <summary>
        ///   Map a view of the file mapping object
        ///   This returns a stream, giving you easy access to the memory,
        ///   as you can use StreamReaders and StreamWriters on top of it
        /// </summary>
        /// <param name="access">desired access to the view</param>
        /// <param name="offset">offset of the file mapping object to 
        ///            start view at</param>
        /// <param name="size">size of the view</param>
        public MapViewStream MapViewAsStream(long offset, long size, long maxViewSize)
        {
            if (!IsOpen)
                throw new ObjectDisposedException("Winterdom.IO.FileMap.MemoryMappedFile.MapView - MMF already closed");

            // sws should verify against _protection
            // Don't know what to do about FILE_MAP_COPY et al

            bool isWriteable = (_protection & MapProtection.PageReadWrite) == MapProtection.PageReadWrite;
            return new MapViewStream(this, offset, size, isWriteable, maxViewSize);
        }

        public MapViewStream MapViewAsStream(long offset, long size)
        {
            return MapViewAsStream(offset, size, MapViewStream.DEF_VIEW_SIZE);
        }

        public MapViewStream MapAsStream(long maxViewSize)
        {
            if (!IsOpen)
                throw new ObjectDisposedException(
                    "Winterdom.IO.FileMap.MemoryMappedFile.MapAsStream - MMF already closed");

            // sws what do we do when the user creates a file mapping of the system paging file
            // do you know how to get its size

            if (FileName == "")
                throw new Exception(
                    "Winterdom.IO.FileMap.MemoryMappedFile.MapAsStream - System paging file cannot be mapped as stream");

            return MapViewAsStream(0, MaxSize, maxViewSize);
        }

        public MapViewStream MapAsStream()
        {
            return MapAsStream(MapViewStream.DEF_VIEW_SIZE);
        }

        public void UnMapView(IntPtr mapBaseAddr)
        {
            Win32MapApis.UnmapViewOfFile(mapBaseAddr);
        }

        public void UnMapView(MapViewStream mappedViewStream)
        {
            UnMapView(mappedViewStream.ViewBaseAddr);
        }

        public void Flush(IntPtr viewBaseAddr, long length)
        {
            IntPtr flushLength = new IntPtr(length);
                // sws note Throws OverflowException if (a) this is a 32-bit platform AND (b) size is out of bounds (ie. int bounds) with respect to this platform
            Win32MapApis.FlushViewOfFile(viewBaseAddr, flushLength);
        }

        public void Flush(MapViewStream mappedViewStream)
        {
            Flush(mappedViewStream.ViewBaseAddr, mappedViewStream.ViewSize);
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsOpen)
                Win32MapApis.CloseHandle(_hMap);
            _hMap = NULL_HANDLE;

            if (disposing)
                GC.SuppressFinalize(this);
        }

        #endregion // IDisposable implementation
    }

    // class MemoryMappedFile
}

// namespace Winterdom.IO.FileMap