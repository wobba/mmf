#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using mAdcOW.DiskStructures.Serializer;

#endregion

namespace mAdcOW.DiskStructures
{
    public class DictionaryFileAccess<TKey, TValue> : IDisposable, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private const uint MaxFileSize = uint.MaxValue;

        private static readonly ISerializeDeserialize<TKey> _keySerializer;
        private static readonly ISerializeDeserialize<TValue> _valueSerializer;
        private readonly string _filePath;

        private readonly Guid _guid = Guid.NewGuid();
        private readonly List<BinaryReader> _valueReaderPool = new List<BinaryReader>(5);
        private int _count;
        private bool _deleteFilesOnExit = true;
        private uint _keyFilePos;
        private Dictionary<int, List<UInt64>> _keyFilePositions;
        private BinaryReader _keyFileReader;
        private BinaryWriter _keyFileWriter;
        private Dictionary<int, DictionaryPage> _keyPageCache;
        private bool _needWriterFlushing;

        private int _pageCount = 6;
        private int _pageSize = 1024 * 1024;
        private uint _valueFilePos;

        private BinaryReader _valueFileReader;
        private BinaryWriter _valueFileWriter;

        private Dictionary<int, DictionaryPage> _valuePageCache;

        public DictionaryFileAccess(string path)
        {
            _filePath = path;
            _keyFilePositions = new Dictionary<int, List<UInt64>>(1000);
            InitWriters();
        }

        #region IDisposable Members

        public void Dispose()
        {
            CloseFiles();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (KeyValuePair<int, List<UInt64>> pair in _keyFilePositions)
            {
                foreach (UInt64 filePos in pair.Value)
                {
                    uint keyPos = (uint)(filePos >> 32);
                    uint valPos = (uint)(filePos & 0x00000000ffffffff);
                    TKey key = GetDictionaryKey(keyPos);
                    TValue value = GetDictionaryValue(valPos);
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<int, List<UInt64>> pair in _keyFilePositions)
            {
                foreach (UInt64 filePos in pair.Value)
                {
                    uint keyPos = (uint)(filePos >> 32);
                    uint valPos = (uint)(filePos & 0x00000000ffffffff);
                    TKey key = GetDictionaryKey(keyPos);
                    TValue value = GetDictionaryValue(valPos);
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        #endregion

        #region properties

        /// <summary>
        /// The number of bytes for a page
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        /// <summary>
        /// Number of pages to keep in memory at once
        /// </summary>
        public int PageCount
        {
            get { return _pageCount; }
            set { _pageCount = value; }
        }

        public int Count
        {
            get { return _count; }
        }

        public bool DeleteFilesOnExit
        {
            get { return _deleteFilesOnExit; }
            set { _deleteFilesOnExit = value; }
        }

        #endregion

        ~DictionaryFileAccess()
        {
            CloseFiles();
        }

        private void CloseFiles()
        {
            if (_valueFileReader != null)
            {
                _valueFileReader.Close();
            }
            if (_keyFileReader != null)
            {
                _keyFileReader.Close();
            }
            if (_keyFileWriter != null)
            {
                _keyFileWriter.Close();
            }
            if (_valueFileWriter != null)
            {
                _valueFileWriter.Close();
            }
            if (DeleteFilesOnExit)
            {
                File.Delete(GetFileName("key"));
                File.Delete(GetFileName("value"));
            }
        }

        private void InitWriters()
        {
            _keyPageCache = new Dictionary<int, DictionaryPage>(_pageCount / 2); // half for keys
            _valuePageCache = new Dictionary<int, DictionaryPage>(_pageCount / 2); // half for values

            if (_keyFileWriter == null)
            {
                FileStream output =
                    new FileStream(GetFileName("key"), FileMode.Create, FileAccess.Write, FileShare.Read, 16384,
                                   FileOptions.SequentialScan);
                _keyFileWriter = new BinaryWriter(output);
            }
            if (_valueFileWriter == null)
            {
                FileStream output =
                    new FileStream(GetFileName("value"), FileMode.Create, FileAccess.Write, FileShare.Read, 16384,
                                   FileOptions.SequentialScan);
                _valueFileWriter = new BinaryWriter(output);
            }
        }

        private BinaryReader GetValueReader(int pageNumber)
        {
            uint startPos = (uint)(pageNumber * _pageSize);
            int bestMatch = -1;
            long bestPos = long.MaxValue;
            lock (_valueReaderPool)
            {
                for (int i = 0; i < _valueReaderPool.Count; i++)
                {
                    long streamPos = _valueReaderPool[i].BaseStream.Position;
                    if (streamPos < startPos && (streamPos - startPos) < bestPos)
                    {
                        bestMatch = i;
                        bestPos = streamPos - startPos;
                    }
                }
            }

            BinaryReader reader;
            if (bestMatch == -1)
            {
                // add a new one
                FileStream input =
                    new FileStream(GetFileName("value"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16384,
                                   FileOptions.SequentialScan);
                reader = new BinaryReader(input);
                _valueReaderPool.Add(reader);
            }
            else
            {
                reader = _valueReaderPool[bestMatch];
            }
            return reader;
        }

        public bool ContainsKey(TKey key)
        {
            List<UInt64> filePositions;
            int dummy;
            return GetKeyFilePositions(key, out filePositions, out dummy, false);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);
            List<UInt64> filePositions;
            bool foundKey = false;
            int matchPos;
            if (GetKeyFilePositions(key, out filePositions, out matchPos, false))
            {
                uint valPos = (uint)(filePositions[matchPos] & 0x00000000ffffffff);

                // Get the data
                value = GetDictionaryValue(valPos);
                foundKey = true;
            }
            return foundKey;
        }

        /// <summary>
        /// Retrieve the file positions for a specific KEY. Either one pos for an exact match,
        /// or a list if the hashcode is equal for several items.
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <param name="filePositions">List of positions for a key</param>
        /// <param name="exactMatchPos">The exact position when there are duplicate hashes</param>
        /// <param name="checkByHashOnly">To check the exact key, or just hash</param>
        /// <returns>If the key exist or not</returns>
        private bool GetKeyFilePositions(TKey key, out List<UInt64> filePositions, out int exactMatchPos,
                                        bool checkByHashOnly)
        {
            bool ret = false;
            exactMatchPos = -1;
            int keyHash = key.GetHashCode();
            if (_keyFilePositions.TryGetValue(keyHash, out filePositions))
            {
                for (int i = 0; i < filePositions.Count; i++)
                {
                    UInt64 filePosition = filePositions[i];
                    uint keyPos = (uint)(filePosition >> 32);

                    TKey dictionaryKey = GetDictionaryKey(keyPos);
                    if (dictionaryKey.Equals(key))
                    {
                        exactMatchPos = i;
                        ret = true;
                        break;
                    }
                }
            }
            if (!ret && !checkByHashOnly)
            {
                filePositions = null;
            }
            return ret;
        }

        private int WriteKey(TKey key, BinaryWriter writer)
        {
            byte[] bytes = _keySerializer.ObjectToBytes(key);
            return WriteBytes(bytes, writer);
        }

        private int WriteValue(TValue value, BinaryWriter writer)
        {
            byte[] bytes = _valueSerializer.ObjectToBytes(value);
            return WriteBytes(bytes, writer);
        }

        /// <summary>
        /// Serialize and write data to a file
        /// </summary>
        /// <param name="bytes">Bytes to be written</param>
        /// <param name="writer">The binary writer for the file</param>
        /// <returns></returns>
        private int WriteBytes(byte[] bytes, BinaryWriter writer)
        {
            if (bytes.Length > _pageSize)
            {
                //PAGE_SIZE = bytes.Length * 4; // store at least 4 objects per page
                throw new OverflowException("Objects cannot be larger than the page size.");
            }

            lock (writer)
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
                _needWriterFlushing = true;
            }
            return bytes.Length;
        }

        private List<UInt64> GetValidKeyFilePositions(TKey key)
        {
            List<UInt64> filePositions;
            int dummy;

            if (GetKeyFilePositions(key, out filePositions, out dummy, true))
            {
                throw new ArgumentException("An item with the same key has already been added.");
            }
            return filePositions;
        }

        private string GetFileName(string name)
        {
            return string.Format(@"{0}\{1}-{2}.bin", _filePath, name, _guid);
        }

        private TKey GetDictionaryKey(uint keyPos)
        {
            int keyPageNum = (int)(keyPos / _pageSize);
            DictionaryPage keyPage;
            if (!_keyPageCache.TryGetValue(keyPageNum, out keyPage))
            {
                // fill cache
                _keyPageCache[keyPageNum] = keyPage = GetPage(keyPageNum, true, ref _keyFileReader, _keyPageCache);
            }
            else
            {
                // if current page is too small, we reget
                if (keyPage.Length + keyPage.StartPosition <= keyPos)
                {
                    _keyPageCache[keyPageNum] = keyPage = GetPage(keyPageNum, true, ref _keyFileReader, _keyPageCache);
                }
            }
            keyPage.LastAccessed = DateTime.UtcNow.Ticks;

            byte[] keyData = GetDataFromPage(keyPos, keyPageNum, keyPage, _keyPageCache, _keyFileReader);
            return _keySerializer.BytesToObject(keyData);
        }

        private TValue GetDictionaryValue(uint valPos)
        {
            int valuePageNum = (int)(valPos / _pageSize);
            DictionaryPage valuePage;

            //            BinaryReader _valueFileReader = GetValueReader(valuePageNum);

            if (!_valuePageCache.TryGetValue(valuePageNum, out valuePage))
            {
                // fill cache
                _valuePageCache[valuePageNum] =
                    valuePage = GetPage(valuePageNum, true, ref _valueFileReader, _valuePageCache);
            }
            else
            {
                // if current page is too small, we reget
                if (valuePage.Length + valuePage.StartPosition <= valPos)
                {
                    _valuePageCache[valuePageNum] =
                        valuePage = GetPage(valuePageNum, true, ref _valueFileReader, _valuePageCache);
                }
            }
            valuePage.LastAccessed = DateTime.UtcNow.Ticks;

            byte[] data = GetDataFromPage(valPos, valuePageNum, valuePage, _valuePageCache, _valueFileReader);
            return _valueSerializer.BytesToObject(data);
        }

        private byte[] GetDataFromPage(uint valPos, int pageNum, DictionaryPage page,
                                       IDictionary<int, DictionaryPage> pageCache, BinaryReader fileReader)
        {
            MemoryStream memStream = new MemoryStream(page.Data);
            BinaryReader memReader = new BinaryReader(memStream);
            int memStartPos = (int)(valPos - (pageNum * _pageSize));
            memReader.BaseStream.Position = memStartPos;

            int objectLength;
            if (memStartPos + 4 > page.Data.Length)
            {
                objectLength =
                    BitConverter.ToInt32(
                        ReadAcrossPageBoundary(4, pageCache, ref memStartPos, memReader, ref fileReader, ref page,
                                               ref pageNum), 0);
            }
            else
            {
                objectLength = memReader.ReadInt32();
            }

            byte[] data;
            if (memStartPos + objectLength > page.Data.Length)
            {
                data =
                    ReadAcrossPageBoundary(objectLength, pageCache, ref memStartPos, memReader, ref fileReader, ref page,
                                           ref pageNum);
            }
            else
            {
                data = memReader.ReadBytes(objectLength);
            }
            return data;
        }

        private byte[] ReadAcrossPageBoundary(int totalBytesToRead, IDictionary<int, DictionaryPage> pageCache,
                                              ref int startPos, BinaryReader memReader, ref BinaryReader fileReader,
                                              ref DictionaryPage page,
                                              ref int pageNum)
        {
            byte[] data = new byte[totalBytesToRead];
            byte[] start = memReader.ReadBytes(page.Data.Length - startPos);
            Array.Copy(start, 0, data, 0, start.Length);
            pageNum++;

            pageCache[pageNum] = page = GetPage(pageNum, false, ref fileReader, pageCache);
            MemoryStream memStream = new MemoryStream(page.Data);
            memReader = new BinaryReader(memStream);
            byte[] theRest = memReader.ReadBytes(totalBytesToRead - start.Length);
            Array.Copy(theRest, 0, data, start.Length, totalBytesToRead - start.Length);
            startPos = theRest.Length + 1;
            return data;
        }

        /// <summary>
        /// Used to get a new page into the cache, or to get the next when we need to read from the next page as well.
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="forceGet">false for ReadAcrossPageBoundary</param>
        /// <param name="fileReader"></param>
        /// <param name="pageCache"></param>
        /// <returns></returns>
        private DictionaryPage GetPage(int pageNum, bool forceGet, ref BinaryReader fileReader,
                                       IDictionary<int, DictionaryPage> pageCache)
        {
            try
            {
                DictionaryPage page = null;
                if (!forceGet)
                {
                    pageCache.TryGetValue(pageNum, out page);
                }

                if (pageCache.Count == 0 || page == null || page.Length != _pageSize)
                {
                    uint startPos = (uint)(pageNum * _pageSize);

                    ReadyStreams();

                    if (startPos > fileReader.BaseStream.Position &&
                        startPos <= fileReader.BaseStream.Position + _pageSize)
                    //if (startPos > fileReader.BaseStream.Position )
                    {
                        // quicker to read than to seek for small amounts (we say one page)
                        int bytesToRead = (int)startPos - (int)fileReader.BaseStream.Position;
                        fileReader.ReadBytes(bytesToRead);
                    }
                    else if (startPos != fileReader.BaseStream.Position)
                    {
                        fileReader.BaseStream.Position = startPos;
                    }
                    byte[] data = fileReader.ReadBytes(_pageSize);
                    page = new DictionaryPage(startPos, data);
                }
                //else
                //{
                //    Trace.WriteLine( "hit");
                //}
                return page;
            }
            finally
            {
                //ExpireCache(pageCache);
                ExpireCache();
            }
        }

        private void ReadyStreams()
        {
            if (_needWriterFlushing)
            {
                lock (_keyFileWriter)
                {
                    _keyFileWriter.Flush();
                }
                lock (_valueFileWriter)
                {
                    _valueFileWriter.Flush();
                }
                _needWriterFlushing = false;
            }

            if (_valueFileReader == null)
            {
                FileStream input =
                    new FileStream(GetFileName("value"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16384,
                                   FileOptions.SequentialScan);
                _valueFileReader = new BinaryReader(input);
            }

            if (_keyFileReader == null)
            {
                FileStream input =
                    new FileStream(GetFileName("key"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16384,
                                   FileOptions.SequentialScan);
                _keyFileReader = new BinaryReader(input);
            }
        }

        private void ExpireCache()
        {
            if (_keyPageCache.Count + _valuePageCache.Count > _pageCount)
            {
                if (_valuePageCache.Count > _keyPageCache.Count)
                {
                    ExpireCache(_valuePageCache, _pageCount - _keyPageCache.Count);
                }
                else
                {
                    ExpireCache(_keyPageCache, _pageCount - _valuePageCache.Count);
                }
            }
        }

        private static void ExpireCache(IDictionary<int, DictionaryPage> cache, int pagesToKeep)
        {
            while (cache.Count > pagesToKeep)
            {
                int pageNo = 0;
                long ticks = long.MaxValue;
                foreach (KeyValuePair<int, DictionaryPage> pair in cache)
                {
                    if (pair.Value.LastAccessed < ticks)
                    {
                        ticks = pair.Value.LastAccessed;
                        pageNo = pair.Key;
                    }
                }
                cache.Remove(pageNo);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (_keyFilePos > MaxFileSize || _valueFilePos > MaxFileSize)
            {
                throw new IndexOutOfRangeException("Adding more data would make the file too large");
            }

            List<UInt64> filePositions = GetValidKeyFilePositions(key) ?? new List<UInt64>(1);

            // Store both file positions in an 64bit value
            UInt64 combinedFilePos = (UInt64)_keyFilePos << 32;
            combinedFilePos += _valueFilePos;

            filePositions.Add(combinedFilePos);
            int keyHash = key.GetHashCode();
            _keyFilePositions[keyHash] = filePositions;

            int length = WriteKey(key, _keyFileWriter);
            // Move the size of the length type(4 bytes) + the actual length
            _keyFilePos += (uint)(Marshal.SizeOf(length) + length);

            length = WriteValue(value, _valueFileWriter);
            // Move the size of the length type(4 bytes) + the actual length
            _valueFilePos += (uint)(Marshal.SizeOf(length) + length);

            _count = Count + 1;
        }

        public bool Remove(TKey key)
        {
            int keyHash = key.GetHashCode();

            List<UInt64> filePositions;
            bool foundKey = false;
            if (_keyFilePositions.TryGetValue(keyHash, out filePositions))
            {
                int removePos = -1;
                for (int i = 0; i < filePositions.Count; i++)
                {
                    UInt64 filePosition = filePositions[i];
                    uint keyPos = (uint)(filePosition >> 32);

                    TKey dictionaryKey = GetDictionaryKey(keyPos);
                    if (!dictionaryKey.Equals(key))
                    {
                        continue;
                    }
                    removePos = i;
                    foundKey = true;
                    break;
                }
                if (!foundKey)
                {
                    return false;
                }

                if (removePos >= 0 && filePositions.Count > 1)
                {
                    //remove this key instance
                    filePositions.RemoveAt(removePos);
                }
                else
                {
                    //since it's only one, we remove it from the position cache entirely
                    return _keyFilePositions.Remove(keyHash);
                }
            }
            return foundKey;
        }

        public List<TKey> AllKeys()
        {
            List<TKey> keys = new List<TKey>(_keyFilePositions.Count);
            foreach (List<UInt64> filePositions in _keyFilePositions.Values)
            {
                foreach (UInt64 filePosition in filePositions)
                {
                    //uint valPos = (uint)(filePosition & 0x00000000ffffffff);
                    uint keyPos = (uint)(filePosition >> 32);

                    TKey dictionaryKey = GetDictionaryKey(keyPos);
                    keys.Add(dictionaryKey);
                }
            }
            return keys;
        }

        public List<TValue> AllValues()
        {
            List<TValue> values = new List<TValue>(_keyFilePositions.Count);
            foreach (List<UInt64> filePositions in _keyFilePositions.Values)
            {
                foreach (UInt64 filePosition in filePositions)
                {
                    uint valPos = (uint)(filePosition & 0x00000000ffffffff);

                    TValue dictionaryValue = GetDictionaryValue(valPos);
                    values.Add(dictionaryValue);
                }
            }
            return values;
        }

        public void Clear()
        {
            _keyFilePositions = new Dictionary<int, List<ulong>>();
            CloseFiles();
            _keyFileWriter = null;
            _valueFileWriter = null;
            InitWriters();
        }

        public bool ByteCompare(TValue source, TValue destination)
        {
            return ByteArrayComparer(_valueSerializer.ObjectToBytes(source), _valueSerializer.ObjectToBytes(destination));
        }

        private bool ByteArrayComparer(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length)
            {
                return false;
            }
            for (int i = arr1.Length; i < arr1.Length; i++)
            {
                if (!arr1[i].Equals(arr2[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}