using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace mAdcOW.DataStructures
{
    /// <summary>
    /// Disk based Dictionary to reduce the amount of RAM used for larger dictionaries.
    /// The maximum datasize for the dictionary is 2^32 bytes.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IDictionaryPersist<TKey, TValue> _persistHandler;

        public Dictionary(IDictionaryPersist<TKey, TValue> persistHandler)
        {
            _persistHandler = persistHandler;
        }

        public Dictionary(string path)
            : this(new DictionaryPersist<TKey, TValue>(path))
        {
        }

        public bool IsStruct { get; set; }

        #region IDictionary<TKey,TValue> Members

        ///<summary>
        ///Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"></see> contains an element with the specified key.
        ///</summary>
        ///
        ///<returns>
        ///true if the <see cref="T:System.Collections.Generic.IDictionary`2"></see> contains an element with the key; otherwise, false.
        ///</returns>
        ///
        ///<param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.</param>
        ///<exception cref="T:System.ArgumentNullException">key is null.</exception>
        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return _persistHandler.ContainsKey(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }


        ///<summary>
        ///Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</summary>
        ///
        ///<param name="value">The object to use as the value of the element to add.</param>
        ///<param name="key">The object to use as the key of the element to add.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"></see> is read-only.</exception>
        ///<exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.</exception>
        ///<exception cref="T:System.ArgumentNullException">key is null.</exception>
        public void Add(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                _persistHandler.Add(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        ///<summary>
        ///Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</summary>
        ///
        ///<returns>
        ///true if the element is successfully removed; otherwise, false.  This method also returns false if key was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</returns>
        ///
        ///<param name="key">The key of the element to remove.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"></see> is read-only.</exception>
        ///<exception cref="T:System.ArgumentNullException">key is null.</exception>
        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                return _persistHandler.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                return _persistHandler.TryGetValue(key, out value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        ///<summary>
        ///Gets or sets the element with the specified key.
        ///</summary>
        ///
        ///<returns>
        ///The element with the specified key.
        ///</returns>
        ///
        ///<param name="key">The key of the element to get or set.</param>
        ///<exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IDictionary`2"></see> is read-only.</exception>
        ///<exception cref="T:System.ArgumentNullException">key is null.</exception>
        ///<exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and key is not found.</exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue val;
                if (TryGetValue(key, out val))
                {
                    return val;
                }
                throw new KeyNotFoundException("The given key was not present in the dictionary.");
            }
            set
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    TValue existing;
                    if (TryGetValue(key, out existing))
                    {
                        if (!_persistHandler.ByteCompare(value, existing))
                        {
                            _lock.EnterWriteLock();
                            try
                            {
                                UpdateItem(key, value);
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                    else
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            Add(key, value);
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }                        
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }
        }

        ///<summary>
        ///Gets an <see cref="T:System.Collections.Generic.ICollection`1"></see> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.Generic.ICollection`1"></see> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</returns>
        ///
        public ICollection<TKey> Keys
        {
            get { return _persistHandler.AllKeys(); }
        }

        ///<summary>
        ///Gets an <see cref="T:System.Collections.Generic.ICollection`1"></see> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.Generic.ICollection`1"></see> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        ///</returns>
        ///
        public ICollection<TValue> Values
        {
            get { return _persistHandler.AllValues(); }
        }

        ///<summary>
        ///Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</summary>
        ///
        ///<param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        ///<summary>
        ///Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</summary>
        ///
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only. </exception>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _persistHandler.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        ///<summary>
        ///Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> contains a specific value.
        ///</summary>
        ///
        ///<returns>
        ///true if item is found in the <see cref="T:System.Collections.Generic.ICollection`1"></see>; otherwise, false.
        ///</returns>
        ///
        ///<param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue checkValue;
            if (TryGetValue(item.Key, out checkValue))
            {
                return checkValue.Equals(item.Value);
            }
            return false;
        }

        ///<summary>
        ///Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"></see> to an <see cref="T:System.Array"></see>, starting at a particular <see cref="T:System.Array"></see> index.
        ///</summary>
        ///
        ///<param name="array">The one-dimensional <see cref="T:System.Array"></see> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"></see>. The <see cref="T:System.Array"></see> must have zero-based indexing.</param>
        ///<param name="index">The zero-based index in array at which copying begins.</param>
        ///<exception cref="T:System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        ///<exception cref="T:System.ArgumentNullException">array is null.</exception>
        ///<exception cref="T:System.ArgumentException">array is multidimensional.-or-arrayIndex is equal to or greater than the length of array.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"></see> is greater than the available space from arrayIndex to the end of the destination array.-or-Type T cannot be cast automatically to the type of the destination array.</exception>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array", "is null");
            }
            if ((index < 0) || (index >= array.Length))
            {
                throw new ArgumentOutOfRangeException("index", "is out of bounds");
            }
            if ((array.Length - index) < Count)
            {
                throw new ArgumentException("array is too small");
            }

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                array[index++] = pair;
            }
        }

        ///<summary>
        ///Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</summary>
        ///
        ///<returns>
        ///true if item was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"></see>; otherwise, false. This method also returns false if item is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</returns>
        ///
        ///<param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
        ///<exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            TValue existing;
            if (TryGetValue(item.Key, out existing))
            {
                if (_persistHandler.ByteCompare(item.Value, existing))
                {
                    Remove(item.Key);
                    return true;
                }
            }
            return false;
        }

        ///<summary>
        ///Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</summary>
        ///
        ///<returns>
        ///The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        ///</returns>
        ///
        public int Count
        {
            get { return _persistHandler.Count; }
        }

        ///<summary>
        ///Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.
        ///</summary>
        ///
        ///<returns>
        ///true if the <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only; otherwise, false.
        ///</returns>
        ///
        public bool IsReadOnly
        {
            get { return false; }
        }


        ///<summary>
        ///Returns an enumerator that iterates through the collection.
        ///</summary>
        ///
        ///<returns>
        ///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = _persistHandler.GetEnumerator();
                 enumerator.MoveNext(); )
            {
                yield return enumerator.Current;
            }
        }

        ///<summary>
        ///Returns an enumerator that iterates through a collection.
        ///</summary>
        ///
        ///<returns>
        ///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        ///</returns>
        ///<filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public bool ContainsValue(TValue value)
        {
            return _persistHandler.ContainsValue(value);
        }

        private void UpdateItem(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                Remove(key);
                Add(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}