using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using mAdcOW.Serializer;

namespace mAdcOW.DataStructures
{
    internal class DictionaryPersist<TKey, TValue> : IDictionaryPersist<TKey, TValue>
    {
        private static ISerializeDeserialize<TKey> _keySerializer;
        private static ISerializeDeserialize<TValue> _valueSerializer;

        private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<KeyValueFileOffset>> _fileOffsets;

        private readonly string _path;
        private int _keyDataSize = -1;
        private int _valueDataSize = -1;
        private Array<byte> _keys;
        private Array<byte> _values;
        private long _largestSeenKeyPosition;
        private long _largestSeenValuePosition;

        public DictionaryPersist(string path, int capacity)
        {
            _fileOffsets = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<KeyValueFileOffset>>(capacity);
            FindKeyValueSize();

            Factory<TKey> keyFactory = new Factory<TKey>();
            Factory<TValue> valueFactory = new Factory<TValue>();
            _keySerializer = keyFactory.GetSerializer();
            _valueSerializer = valueFactory.GetSerializer();

            _path = path;
            _keys = new Array<byte>(1000, path, true);
            _values = new Array<byte>(1000, path, true);
        }

        public DictionaryPersist(string path)
            : this(path, 1000000)
        {
        }

        #region IDictionaryPersist<TKey,TValue> Members

        public int Count { get; set; }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (System.Collections.Generic.List<KeyValueFileOffset> offsets in _fileOffsets.Values)
            {
                foreach (KeyValueFileOffset fileOffset in offsets)
                {
                    TKey key = ReadDictionaryKey(fileOffset);
                    TValue value = ReadDictionaryValue(fileOffset);
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            int keyHash = key.GetHashCode();
            return _fileOffsets.ContainsKey(keyHash);
        }

        public bool ContainsValue(TValue value)
        {
            foreach (TValue listValue in AllValues())
            {
                if (listValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            System.Collections.Generic.List<KeyValueFileOffset> keyFilePositions = null;

            bool keyExist = ActOnKeyExist((keyPositions, index) =>
                                              {
                                                  throw new ArgumentException(
                                                      "An item with the same key has already been added.");
                                              }, key);

            if (!keyExist)
            {
                if (!_fileOffsets.TryGetValue(key.GetHashCode(), out keyFilePositions))
                {
                    _fileOffsets[key.GetHashCode()] =
                        keyFilePositions = new System.Collections.Generic.List<KeyValueFileOffset>(1);
                }
            }
            Persist(key, value, keyFilePositions);
            Count++;
        }

        public bool Remove(TKey key)
        {
            bool removed = ActOnKeyExist((keyPositions, index) =>
                                             {
                                                 keyPositions.RemoveAt(index);
                                                 return default(TValue);
                                             }, key);
            if (removed)
            {
                Count--;
            }
            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TValue dictionaryValue = default(TValue);
            bool result =
                ActOnKeyExist((keyPositions, index) => dictionaryValue = ReadDictionaryValue(keyPositions[index]), key);
            value = dictionaryValue;
            return result;
        }

        public bool ByteCompare(TValue value, TValue existing)
        {
            return ByteArrayCompare.UnSafeEquals(_valueSerializer.ObjectToBytes(value),
                                                 _valueSerializer.ObjectToBytes(existing));
        }

        public ICollection<TKey> AllKeys()
        {
            System.Collections.Generic.List<TKey> keys = new System.Collections.Generic.List<TKey>(_fileOffsets.Count);

            foreach (System.Collections.Generic.List<KeyValueFileOffset> offsets in _fileOffsets.Values)
            {
                foreach (KeyValueFileOffset fileOffset in offsets)
                {
                    TKey key = ReadDictionaryKey(fileOffset);
                    keys.Add(key);
                }
            }
            return keys;
        }

        public ICollection<TValue> AllValues()
        {
            System.Collections.Generic.List<TValue> values =
                new System.Collections.Generic.List<TValue>(_fileOffsets.Count);

            foreach (System.Collections.Generic.List<KeyValueFileOffset> offsets in _fileOffsets.Values)
            {
                foreach (KeyValueFileOffset fileOffset in offsets)
                {
                    TValue value = ReadDictionaryValue(fileOffset);
                    values.Add(value);
                }
            }
            return values;
        }

        public void Clear()
        {
            _fileOffsets.Clear();
            _keys = new Array<byte>(1000, _path, true);
            _values = new Array<byte>(1000, _path, true);
            _largestSeenKeyPosition = 0;
            _largestSeenValuePosition = 0;
            Count = 0;
        }

        #endregion

        ~DictionaryPersist()
        {
            if (_keys != null)
                _keys.Dispose();
            if (_values != null)
                _values.Dispose();
        }

        private void FindKeyValueSize()
        {
            try
            {
                _keyDataSize = Marshal.SizeOf(typeof(TKey));
                _valueDataSize = Marshal.SizeOf(typeof(TValue));
            }
            catch (Exception)
            {
                _keyDataSize = -1;
                _valueDataSize = -1;
            }
        }

        public bool ByteCompare(TKey key, TKey existing)
        {
            return ByteArrayCompare.UnSafeEquals(_keySerializer.ObjectToBytes(key),
                                                 _keySerializer.ObjectToBytes(existing));
        }

        private void Persist(TKey key, TValue value, IList<KeyValueFileOffset> keyFilePositions)
        {
            KeyValueFileOffset positions = new KeyValueFileOffset
                                               {
                                                   KeyPosition =
                                                       _keyDataSize == -1
                                                           ? _largestSeenKeyPosition
                                                           : _largestSeenKeyPosition / _keyDataSize,
                                                   ValuePosition =
                                                       _valueDataSize == -1
                                                           ? _largestSeenValuePosition
                                                           : _largestSeenValuePosition / _valueDataSize
                                               };

            keyFilePositions.Add(positions);
            WriteKey(key, positions);
            WriteValue(value, positions);
        }

        private int FindKeyPosition(TKey key, IList<KeyValueFileOffset> keyFilePositions)
        {
            for (int i = 0; i < keyFilePositions.Count; i++)
            {
                _keys.Position = _keyDataSize == -1
                                     ? keyFilePositions[i].KeyPosition
                                     : keyFilePositions[i].KeyPosition * _keyDataSize;
                int length = _keyDataSize == -1 ? BitConverter.ToInt32(_keys.MultiRead(4), 0) : _keyDataSize;
                TKey dictionaryKey = _keySerializer.BytesToObject(_keys.MultiRead(length));
                //if (dictionaryKey.Equals(key))
                if (ByteCompare(dictionaryKey, key))
                {
                    return i;
                }
            }
            return -1;
        }

        private void WriteKey(TKey key, KeyValueFileOffset offset)
        {
            long writePostion = _keyDataSize == -1 ? offset.KeyPosition : offset.KeyPosition * _keyDataSize;
            _keys.Position = writePostion;

            byte[] keyBytes = _keySerializer.ObjectToBytes(key);
            int writeLength = _keyDataSize;
            if (_keyDataSize == -1)
            {
                _keys.Write(BitConverter.GetBytes(keyBytes.Length));
                writeLength = keyBytes.Length + sizeof(int);
            }
            _keys.Write(keyBytes);
            if (WritePositionWithinCurrentFile(writePostion, writeLength, _largestSeenKeyPosition)) return;
            _largestSeenKeyPosition += writeLength;
            CheckForMaxFileSizeOn32BitSystem(_largestSeenKeyPosition);
        }

        private void WriteValue(TValue value, KeyValueFileOffset offset)
        {
            long writePostion = _valueDataSize == -1 ? offset.ValuePosition : offset.ValuePosition * _valueDataSize;
            _values.Position = writePostion;
            byte[] valueBytes = _valueSerializer.ObjectToBytes(value);
            int writeLength = _valueDataSize;
            if (_valueDataSize == -1)
            {
                _values.Write(BitConverter.GetBytes(valueBytes.Length));
                writeLength = valueBytes.Length + sizeof(int);
            }
            _values.Write(valueBytes);
            if (WritePositionWithinCurrentFile(writePostion, writeLength, _largestSeenValuePosition)) return;
            _largestSeenValuePosition += writeLength;
            CheckForMaxFileSizeOn32BitSystem(_largestSeenValuePosition);
        }

        private bool WritePositionWithinCurrentFile(long offset, int writeLength, long largestSeenFilePosition)
        {
            return offset + writeLength < largestSeenFilePosition;
        }

        private void CheckForMaxFileSizeOn32BitSystem(long filePosition)
        {
            if (IntPtr.Size == 4 && filePosition > Int32.MaxValue)
            {
                throw new IndexOutOfRangeException("Adding more data would make the file too large");
            }
        }

        private bool ActOnKeyExist(Func<System.Collections.Generic.List<KeyValueFileOffset>, int, TValue> func, TKey key)
        {
            int keyHash = key.GetHashCode();
            System.Collections.Generic.List<KeyValueFileOffset> keyFilePositions;
            if (_fileOffsets.TryGetValue(keyHash, out keyFilePositions))
            {
                int keyPosition = FindKeyPosition(key, keyFilePositions);
                if (keyPosition >= 0)
                {
                    func(keyFilePositions, keyPosition);
                    return true;
                }
            }
            return false;
        }

        private TKey ReadDictionaryKey(KeyValueFileOffset fileOffset)
        {
            int length;
            if (_keyDataSize == -1)
            {
                _keys.Position = fileOffset.KeyPosition;
                length = BitConverter.ToInt32(_keys.MultiRead(4), 0);
            }
            else
            {
                _keys.Position = fileOffset.KeyPosition * _keyDataSize;
                length = _keyDataSize;
            }
            return _keySerializer.BytesToObject(_keys.MultiRead(length));
        }

        private TValue ReadDictionaryValue(KeyValueFileOffset fileOffset)
        {
            int length;
            if (_valueDataSize == -1)
            {
                _values.Position = fileOffset.ValuePosition;
                length = BitConverter.ToInt32(_values.MultiRead(4), 0);
            }
            else
            {
                _values.Position = fileOffset.ValuePosition * _valueDataSize;
                length = _keyDataSize;
            }
            return _valueSerializer.BytesToObject(_values.MultiRead(length));
        }
    }
}