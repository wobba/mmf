//
// MapViewStream.cs
//    
//    Implementation of a library to use Win32 Memory Mapped
//    Files from within .NET applications
//
// COPYRIGHT (C) 2001, Tomas Restrepo (tomasr@mvps.org)
//   Original concept and implementation
// COPYRIGHT (C) 2006, Steve Simpson (s.simpson64@gmail.com)
//   Modifications to allow dynamic paging for seek, read, and write methods
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Winterdom.IO.FileMap
{
    /// <summary>
    ///   Allows you to read/write from/to
    ///   a view of a memory mapped file.
    /// </summary>
    public class MapViewStream : Stream, IDisposable
    {
        #region consts

        public const long DEF_ALLOC_GRANULARITY = 0x20000;
                          // sws 128kB this should be safe for both 32-bit and 64-bit systems, otherwise we can call GetSystemInfo API

        public const long DEF_VIEW_SIZE = 32*1024*1024; // sws 32MB, what's an appropriate default??
        public const long MIN_VIEW_SIZE = DEF_ALLOC_GRANULARITY;
        // sws MAX_VIEW_SIZE, should really query system for amount of phys ram and do something smart
        protected const long MAX_VIEW_SIZE = ((long) int.MaxValue) - (100*1024*1024);
                             // About 1.9GB.  Win32 is limited to 2GB of commitable RAM per process.. You need some ram for heap and thread stacks

        #endregion

        #region Map/View Related Fields

        // MemoryMappedFile *friend* stuff

        protected MemoryMappedFile _backingFile; // sws should these have public read props
        protected MapAccess _access = MapAccess.FileMapWrite;

        protected bool _isWriteable;

        // Pointer to the base address of the currently mapped view

        private IntPtr _viewBaseAddr = IntPtr.Zero;

        public IntPtr ViewBaseAddr
        {
            get { return _viewBaseAddr; }
        }

        protected long _defViewSize = DEF_VIEW_SIZE;
        protected long _allocGranularity = DEF_ALLOC_GRANULARITY;

        protected long _mapStartIdx;

        protected long _minMapStartIdx
        {
            get { return (_mapStartIdx/_allocGranularity)*_allocGranularity; }
        }

        protected long _mapSize;

        protected long _viewStartIdx = -1;

        public long ViewStartIdx
        {
            get { return _viewStartIdx; }
        }

        public long ViewStopIdx
        {
            get { return (_viewStartIdx + _viewSize - 1); }
        }

        protected long _viewSize = -1;

        public long ViewSize
        {
            get { return _viewSize; }
        }

        protected long _viewPosition = -1;

        public long ViewPosition
        {
            get { return _viewPosition; }
        }

        public bool IsViewMapped
        {
            get { return (_viewStartIdx >= _mapStartIdx) && ((_viewStartIdx + _viewSize) <= (_mapStartIdx + _mapSize)); }
        }

        #endregion // Map/View Related Fields

        #region Map / Unmap View

        #region Unmap View

        protected void UnmapView()
        {
            if (IsViewMapped)
            {
                _backingFile.UnMapView(this);
                _viewStartIdx = -1;
                _viewSize = -1;
                _viewPosition = -1;
            }
        }

        #endregion

        #region Map View

        protected void MapView(ref long viewStartIdx, ref long viewSize)
        {
            if ((viewStartIdx < _mapStartIdx) || (viewStartIdx > (_mapStartIdx + _mapSize)))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - viewStartIdx is invalid.  viewStartIdx == {0}, _mapStartIdx == {1}, _mapSize == {2}",
                        viewStartIdx, _mapStartIdx, _mapSize));
            }
            if ((viewSize < 1) || (viewSize > _defViewSize))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - viewSize is invalid.  viewSize == {0}, _defViewSize == {1}",
                        viewSize, _defViewSize));
            }

            // Trim End

            if ((viewStartIdx + viewSize) > (_mapStartIdx + _mapSize))
            {
                viewSize = (_mapStartIdx + _mapSize) - viewStartIdx;
            }

            long positionAdjustment = viewStartIdx%_allocGranularity;

            if (positionAdjustment != 0)
            {
                viewSize = viewSize + positionAdjustment;
                //viewStartIdx = (viewStartIdx / _allocGranularity) * _allocGranularity;
                viewStartIdx = viewStartIdx - positionAdjustment;
            }

            // Unmap existing view if different from this view..

            if (IsViewMapped && ((viewStartIdx != _viewStartIdx) || (viewSize != _viewSize)))
            {
                UnmapView();
            }

            // Now map the view

            _viewBaseAddr = _backingFile.MapView(_access, viewStartIdx, viewSize);
            _viewStartIdx = viewStartIdx;
            _viewSize = viewSize;
        }

        protected void MapView(ref long viewStartIdx)
        {
            long viewSize = _defViewSize;
            MapView(ref viewStartIdx, ref viewSize);
        }

        protected void MapCentredView(ref long viewCentreIdx, ref long viewSize)
        {
            if ((viewCentreIdx < _mapStartIdx) || (viewCentreIdx > (_mapStartIdx + _mapSize)))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - viewCentreIdx is invalid.  viewCentreIdx == {0}, _mapStartIdx == {1}, _mapSize == {2}",
                        viewCentreIdx, _mapStartIdx, _mapSize));
            }
            if ((viewSize < 1) || (viewSize > _defViewSize))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - viewSize is invalid.  viewSize == {0}, _defViewSize == {1}",
                        viewSize, _defViewSize));
            }

            // Centre

            long viewStartIdx = viewCentreIdx - (viewSize/2);

            // Trim Start

            if (viewStartIdx < _mapStartIdx)
            {
                viewStartIdx = _mapStartIdx;
            }

            // Trim End

            if ((viewStartIdx + viewSize) > (_mapStartIdx + _mapSize))
            {
                viewSize = (_mapStartIdx + _mapSize) - viewStartIdx;
            }

            MapView(ref viewStartIdx, ref viewSize);

            // Sanity check..

            Debug.Assert(viewStartIdx >= _mapStartIdx);
            Debug.Assert((viewStartIdx + viewSize) <= (_mapStartIdx + _mapSize));
            Debug.Assert((viewStartIdx <= viewCentreIdx) && (viewCentreIdx <= (viewStartIdx + viewSize)));

            // Assign refs

            viewCentreIdx = viewStartIdx + (viewSize/2);
        }

        protected void MapCentredView(ref long viewCentreIdx)
        {
            long viewSize = _defViewSize;
            MapCentredView(ref viewCentreIdx, ref viewSize);
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor used internally by MemoryMappedFile.
        /// </summary>
        /// <param name="backingFile">Preconstructed MemoryMappedFile</param>
        /// <param name="mapStartIdx">Index in the backingFile at which the view starts</param>
        /// <param name="mapSize">Size of the view, in bytes.</param>
        /// <param name="isWriteable">True if Read/Write access is desired, False otherwise</param>
        internal MapViewStream(MemoryMappedFile backingFile, long mapStartIdx, long mapSize, bool isWriteable,
                               long defViewSize)
        {
            if (backingFile == null)
            {
                throw new Exception("MapViewStream.MapViewStream - backingFile is null");
            }
            if (!backingFile.IsOpen)
            {
                throw new Exception("MapViewStream.MapViewStream - backingFile is not open");
            }
            if ((mapStartIdx < 0) || (mapStartIdx > backingFile.MaxSize))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - mapStartIdx is invalid.  mapStartIdx == {0}, backingFile.MaxSize == {1}",
                        mapStartIdx, backingFile.MaxSize));
            }
            if ((mapSize < 1) || ((mapStartIdx + mapSize) > backingFile.MaxSize))
            {
                throw new Exception(
                    string.Format(
                        "MapViewStream.MapViewStream - mapSize is invalid.  mapStartIdx == {0}, mapSize == {1}, backingFile.MaxSize == {2}",
                        mapStartIdx, mapSize, backingFile.MaxSize));
            }
            if ((defViewSize < MIN_VIEW_SIZE) || (defViewSize > MAX_VIEW_SIZE))
                // sws debug fix hack with appropriate OS RAM query
            {
                throw new Exception(
                    string.Format("MapViewStream.MapViewStream - defViewSize is invalid.  defViewSize == {0}",
                                  defViewSize));
            }

            _backingFile = backingFile;
            _isWriteable = isWriteable;
            _access = isWriteable ? MapAccess.FileMapWrite : MapAccess.FileMapRead;
            // Need a backingFile.SupportsAccess function that takes a MapAccess compares it against its stored MapProtection protection and returns bool

            _defViewSize = defViewSize;
            _mapStartIdx = mapStartIdx;
            _mapSize = mapSize;

            _isOpen = true;

            // Map the first view

            Seek(0, SeekOrigin.Begin);
        }

        internal MapViewStream(MemoryMappedFile backingFile, long mapStartIdx, long mapSize, bool isWriteable) :
            this(backingFile, mapStartIdx, mapSize, isWriteable, DEF_VIEW_SIZE)
        {
        }

        #endregion

        #region Stream Properties

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return _isWriteable; }
        }

        public override long Length
        {
            get { return _mapSize; }
        }

        //! our current position in the stream buffer
        private long _position;

        public override long Position
        {
            get { return _position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        #endregion // Stream Properties

        #region Stream Methods

        public override void Flush()
        {
            if (!IsOpen)
                throw new ObjectDisposedException("Winterdom.IO.FileMap.MapViewStream.Flush - Stream is closed");

            // flush the view but leave the buffer intact
            _backingFile.Flush(this);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new ObjectDisposedException("Winterdom.IO.FileMap.MapViewStream.Read - Stream is closed");

            if (buffer.Length - offset < count)
                throw new ArgumentException("Winterdom.IO.FileMap.MapViewStream.Read - Invalid Offset");

            long bytesToRead = Math.Min(Length - _position, count);

            long numBytesRemainingInCurMap = ViewSize - _viewPosition;

            if (bytesToRead <= numBytesRemainingInCurMap)
            {
                // Required data is contained completely in currently mapped view

                // Read data from map

                Marshal.Copy((IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition), buffer, offset, (int) bytesToRead);
                _viewPosition += bytesToRead;
                _position += bytesToRead;
            }
            else
            {
                // Required data is only partly contained in currently mapped view ==> remap required

                long bytesToReadInCurMap = numBytesRemainingInCurMap;
                long bytesToReadInLastReMap = (bytesToRead - numBytesRemainingInCurMap)%ViewSize;
                bool isLastMapPartialRead = bytesToReadInLastReMap > 0;
                int numReMapsReqd = (int) ((bytesToRead - bytesToReadInCurMap)/ViewSize) +
                                    (isLastMapPartialRead ? 1 : 0);

                for (int i = 0; i < numReMapsReqd; i++)
                {
                    // Read data from map

                    Marshal.Copy((IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition), buffer, offset,
                                 (int) bytesToReadInCurMap);
                    _position += bytesToReadInCurMap;
                    _viewPosition += bytesToReadInCurMap;
                    offset += (int) bytesToReadInCurMap;

                    // Remap

                    Seek(ViewStopIdx + 1, SeekOrigin.Begin, true, false);
                    bytesToReadInCurMap = ViewSize;
                }


                if (isLastMapPartialRead)
                {
                    // Read any dag from last view to be mapped

                    bytesToReadInCurMap = bytesToReadInLastReMap;
                    Marshal.Copy((IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition), buffer, offset,
                                 (int) bytesToReadInCurMap);
                    _position += bytesToReadInCurMap;
                    _viewPosition += bytesToReadInCurMap;
                    offset += (int) bytesToReadInCurMap;
                }
            }

            return (int) bytesToRead;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!IsOpen)
                throw new ObjectDisposedException("Winterdom.IO.FileMap.MapViewStream.Write - Stream is closed");

            if (!CanWrite)
                throw new FileMapIOException("Winterdom.IO.FileMap.MapViewStream.Write - Stream cannot be written to");

            if (buffer.Length - offset < count)
                throw new ArgumentException("Winterdom.IO.FileMap.MapViewStream.Write - Invalid Offset");

            long bytesToWrite = Math.Min(Length - _position, count);

            if (bytesToWrite == 0)
                return;

            long numBytesRemainingInCurMap = ViewSize - _viewPosition;

            if (bytesToWrite <= numBytesRemainingInCurMap)
            {
                // Data is contained completely in currently mapped view

                // Write data to map

                Marshal.Copy(buffer, offset, (IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition), (int) bytesToWrite);
                _viewPosition += bytesToWrite;
                _position += bytesToWrite;
            }
            else
            {
                // Data is only partly contained in currently mapped view ==> remap required

                long bytesToWriteInCurMap = numBytesRemainingInCurMap;
                long bytesToWriteInLastReMap = (bytesToWrite - numBytesRemainingInCurMap)%ViewSize;
                bool isLastMapPartialWrite = bytesToWriteInLastReMap > 0;
                int numReMapsReqd =
                    (int) ((bytesToWrite - bytesToWriteInCurMap)/ViewSize + (isLastMapPartialWrite ? 1 : 0));

                for (int i = 0; i < numReMapsReqd; i++)
                {
                    // Write data to map

                    Marshal.Copy(buffer, offset, (IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition),
                                 (int) bytesToWriteInCurMap);
                    _position += bytesToWriteInCurMap;
                    _viewPosition += bytesToWriteInCurMap;
                    offset += (int) bytesToWriteInCurMap;

                    // Remap

                    Seek(ViewStopIdx + 1, SeekOrigin.Begin, true, false);
                    bytesToWriteInCurMap = ViewSize;
                }


                if (isLastMapPartialWrite)
                {
                    // Write any dag to last view to be mapped

                    bytesToWriteInCurMap = bytesToWriteInLastReMap;

                    Marshal.Copy(buffer, offset, (IntPtr) (_viewBaseAddr.ToInt64() + _viewPosition),
                                 (int) bytesToWriteInCurMap);
                    _position += bytesToWriteInCurMap;
                    _viewPosition += bytesToWriteInCurMap;
                    offset += (int) bytesToWriteInCurMap;
                }
            }
        }

        public long Seek(long offset, SeekOrigin origin, bool ForceRemap, bool CentreRemap)
        {
            long newpos = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newpos = offset;
                    break;
                case SeekOrigin.Current:
                    newpos = Position + offset;
                    break;
                case SeekOrigin.End:
                    newpos = Length + offset;
                    break;
            }

            // sanity check
            if (newpos < 0 || newpos > Length)
                throw new FileMapIOException("Winterdom.IO.FileMap.MapViewStream.Seek - Invalid Seek Offset");

            // Check if we need to remap view

            if (ForceRemap || (newpos < ViewStartIdx) || (newpos > ViewStopIdx) || !IsViewMapped)
            {
                if (CentreRemap)
                {
                    long viewCentreIdx = newpos;
                    MapCentredView(ref viewCentreIdx);
                }
                else
                {
                    long viewStartIdx = newpos;
                    MapView(ref viewStartIdx);
                }
            }

            _position = newpos;
            _viewPosition = _position - ViewStartIdx;

            return newpos;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Seek(offset, origin, false, true);
        }

        public override void SetLength(long value)
        {
            // not supported!
            throw new NotSupportedException("Winterdom.IO.FileMap.MapViewStream.SetLength - Can't change map size");
        }

        public override void Close()
        {
            Dispose(true);
        }

        #endregion // Stream methods

        #region IDisposable Implementation

        private bool _isOpen;

        public bool IsOpen
        {
            get { return _isOpen; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsOpen)
            {
                Flush();
                UnmapView();
                _isOpen = false;
            }

            if (disposing)
                GC.SuppressFinalize(this);
        }

        ~MapViewStream()
        {
            Dispose(false);
        }

        #endregion // IDisposable Implementation
    }

    // class MapViewStream
}

// namespace Winterdom.IO.FileMap