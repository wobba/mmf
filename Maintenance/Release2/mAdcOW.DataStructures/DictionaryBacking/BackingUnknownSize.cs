using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using mAdcOW.Serializer;

namespace mAdcOW.DataStructures.DictionaryBacking
{
    public class BackingUnknownSize<TKey, TValue> : IDictionaryPersist<TKey, TValue>
    {
        private static readonly byte[] _emptyPosition = new byte[9];
        private static ISerializeDeserialize<TKey> _keySerializer;
        private static ISerializeDeserialize<TValue> _valueSerializer;
        private readonly int _capacity;

        private Array<long> _hashCodeLookup;
        private ByteArray _keys;
        private ByteArray _values;
        private int _firstItem = -1;
        private long _largestSeenKeyPosition = 1; // set start position to 1 to simplify logic
        private long _largestSeenValuePosition;
        private string _path;
        private int _defaultKeySize;
        private int _defaultValueSize;

        public BackingUnknownSize(string path, int capacity)
        {
            _capacity = HashHelpers.GetPrime(capacity);
            _path = path;

            Factory<TKey> keyFactory = new Factory<TKey>();
            Factory<TValue> valueFactory = new Factory<TValue>();
            _keySerializer = keyFactory.GetSerializer();
            _valueSerializer = valueFactory.GetSerializer();

            SetDefaultKeyValueSize();

            _hashCodeLookup = new Array<long>(_capacity, path, true);

            _keys = new ByteArray(_capacity * _defaultKeySize, path, true);
            _values = new ByteArray(_capacity * _defaultValueSize, path, true);
        }

        void SetDefaultKeyValueSize()
        {
            if( default(TKey) != null )
                _defaultKeySize = _keySerializer.ObjectToBytes(default(TKey)).Length;
            if (default(TValue) != null)
                _defaultValueSize = _valueSerializer.ObjectToBytes(default(TValue)).Length;
            
            if (_defaultKeySize == 0) _defaultKeySize = 40;
            if (_defaultValueSize == 0) _defaultValueSize = 40;
        }


        ~BackingUnknownSize()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
                if (_keys != null)
                    _keys.Dispose();
                if (_values != null)
                    _values.Dispose();
                if (_hashCodeLookup != null)
                    _hashCodeLookup.Dispose();
            }
        }

        private int GetHashCodePosition(TKey key)
        {
            int num = key.GetHashCode() & 0x7fffffff;
            int index = num % _capacity;
            return index;
        }

        #region Implementation of IEnumerable

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (long firstKeyPosition in _hashCodeLookup)
            {
                long keyPosition = firstKeyPosition;
                while (keyPosition != 0)
                {
                    _keys.Position = keyPosition;
                    int keyLength = _keys.ReadVInt();
                    TKey key = _keySerializer.BytesToObject(_keys.MultiRead(keyLength));
                    long valuePos = _keys.ReadVLong();
                    _values.Position = valuePos;
                    int valueLength = _values.ReadVInt();
                    TValue value = _valueSerializer.BytesToObject(_values.MultiRead(valueLength));
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                    keyPosition = _keys.ReadVLong();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of IDictionaryPersist<TKey,TValue>

        public int Count { get; set; }

        public bool ContainsKey(TKey key)
        {
            TValue value;
            return TryGetValue(key, out value);
        }

        public bool ContainsValue(TValue value)
        {
            byte[] valueBytes = _valueSerializer.ObjectToBytes(value);

            IEnumerator<KeyValuePair<TKey, TValue>> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (ByteCompare(valueBytes, enumerator.Current.Value)) return true;
            }
            return false;
        }

        /*
        File Layout Keyfile
        
        KeyLength   VInt
        KeyBytes[]
        ValuePos    VLong
        NextKeyPos  VLong - but occupies 9 bytes - might change back to 8 and no compression
        
        File Layout Valuefile
        ValueLength   VInt
        ValueBytes[]
         */

        public void Add(TKey key, TValue value)
        {
            var pos = GetHashCodePosition(key);
            long keyFilePos = _hashCodeLookup[pos];

            byte[] keyBytes = _keySerializer.ObjectToBytes(key);
            if (keyFilePos != 0)
            {
                long nextKeyPos;
                do
                {
                    _keys.Position = keyFilePos;
                    int keyLength = _keys.ReadVInt();
                    if (ByteArrayCompare.Equals(keyBytes, _keys.MultiRead(keyLength)))
                    {
                        throw new ArgumentException("An item with the same key has already been added.");
                    }
                    _keys.ReadVLong(); // skip valuepos
                    nextKeyPos = _keys.Position;
                    keyFilePos = _keys.ReadVLong(); // next key pos with same hash
                } while (keyFilePos != 0);
                _keys.Position = nextKeyPos;
                _keys.WriteVLong(_largestSeenKeyPosition); // Fill in the chained keyhash
            }
            else
            {
                _hashCodeLookup[pos] = _largestSeenKeyPosition;
            }
            if (_firstItem == -1)
            {
                _firstItem = pos;
            }

            _keys.Position = _largestSeenKeyPosition;
            byte len = _keys.WriteVInt(keyBytes.Length);
            _keys.Write(keyBytes);
            len += _keys.WriteVLong(_largestSeenValuePosition);
            _keys.Write(_emptyPosition); // space for nextkeypos

            _largestSeenKeyPosition += keyBytes.Length + len + 9;

            byte[] valueBytes = _valueSerializer.ObjectToBytes(value);

            _values.Position = _largestSeenValuePosition;
            len = _values.WriteVInt(valueBytes.Length);
            _values.Write(valueBytes);
            _largestSeenValuePosition += valueBytes.Length + len;

            Count++;
        }

        public bool Remove(TKey key)
        {
            var pos = GetHashCodePosition(key);
            long nextKeyFilePos = _hashCodeLookup[pos];

            byte[] keyBytes = _keySerializer.ObjectToBytes(key);
            long updateKeyPos = nextKeyFilePos;
            if (nextKeyFilePos != 0)
            {
                int keysWithSameHash = 0;
                do
                {
                    keysWithSameHash++;
                    _keys.Position = nextKeyFilePos;

                    int keyLength = _keys.ReadVInt();
                    var readKeyBytes = _keys.MultiRead(keyLength);

                    _keys.ReadVLong(); // skip valuepos

                    long newUpdateKeyPos = _keys.Position; // store pos space for chained key pos

                    nextKeyFilePos = _keys.ReadVLong(); // next key pos with same hash

                    if (ByteArrayCompare.Equals(keyBytes, readKeyBytes))
                    {
                        if (keysWithSameHash == 1)
                        {
                            _hashCodeLookup[pos] = nextKeyFilePos;
                        }
                        else
                        {
                            _keys.Position = updateKeyPos;
                            _keys.WriteVLong(nextKeyFilePos);
                        }
                        Count--;
                        return true;
                    }
                    updateKeyPos = newUpdateKeyPos;

                } while (nextKeyFilePos != 0);
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var pos = GetHashCodePosition(key);
            long keyFilePos = _hashCodeLookup[pos];

            byte[] keyBytes = _keySerializer.ObjectToBytes(key);
            if (keyFilePos != 0)
            {
                do
                {
                    _keys.Position = keyFilePos;
                    int keyLength = _keys.ReadVInt();
                    if (ByteArrayCompare.Equals(keyBytes, _keys.MultiRead(keyLength)))
                    {
                        // we have match on the key
                        _values.Position = _keys.ReadVLong();
                        int length = _values.ReadVInt();
                        value = _valueSerializer.BytesToObject(_values.MultiRead(length));
                        return true;
                    }
                    _keys.ReadVLong(); // skip valueposition
                    keyFilePos = _keys.ReadVLong(); // next key pos with same hash
                } while (keyFilePos != 0);
            }

            value = default(TValue);
            return false;
        }

        public bool ByteCompare(TValue value, TValue existing)
        {
            return ByteArrayCompare.UnSafeEquals(_valueSerializer.ObjectToBytes(value),
                                                 _valueSerializer.ObjectToBytes(existing));

        }

        public bool ByteCompare(byte[] value, TValue existing)
        {
            return ByteArrayCompare.UnSafeEquals(value, _valueSerializer.ObjectToBytes(existing));
        }

        public IEnumerable<TKey> AllKeys()
        {
            foreach (long firstKeyPosition in _hashCodeLookup)
            {
                long keyPosition = firstKeyPosition;
                while (keyPosition != 0)
                {
                    _keys.Position = keyPosition;
                    int keyLength = _keys.ReadVInt();
                    TKey key = _keySerializer.BytesToObject(_keys.MultiRead(keyLength));
                    _keys.ReadVLong(); // skip value pos
                    yield return key;
                    keyPosition = _keys.ReadVLong();
                }
            }
        }

        public IEnumerable<TValue> AllValues()
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current.Value;
            }
        }

        public void Clear()
        {
            _hashCodeLookup = new Array<long>(_capacity, _path, true);
            _keys = new ByteArray(_capacity * _defaultKeySize, _path, true);
            _values = new ByteArray(_capacity * _defaultValueSize, _path, true);
            _largestSeenKeyPosition = 1;
            _largestSeenValuePosition = 0;
            Count = 0;
        }
        #endregion
    }
}