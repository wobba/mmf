#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using mAdcOW.Serializer;

#endregion

namespace mAdcOW.DataStructures
{
    public class List<TValue> : Array<TValue>, IList<TValue>
        where TValue : struct
    {
        private readonly ReaderWriterLockSlim _valueLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Create a new memory mapped List on disk
        /// </summary>
        /// <param name="capacity">The initial capacity of the list to allocate on disk</param>
        /// <param name="path">The directory where the memory mapped file is to be stored</param>
        public List(long capacity, string path)
            : base(capacity, path, false, new Factory<TValue>().GetSerializer(), new ViewManager())
        {
            _valueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public override long Length
        {
            get { return Count; }
        }

        #region IList<T> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        ///                 </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///                 </exception>
        public void Add(TValue item)
        {
            AutoGrow = true;
            base[Count] = item;
            AutoGrow = false;
            Count++;
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. 
        ///                 </exception>
        public void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        ///                 </param>
        public bool Contains(TValue item)
        {
            _valueLock.EnterReadLock();
            try
            {
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].Equals(item))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                _valueLock.EnterReadLock();
            }
            return false;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.
        ///                 </param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.
        ///                 </param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.
        ///                 </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.
        ///                 </exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
        ///                     -or-
        ///                 <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
        ///                     -or-
        ///                     The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        ///                     -or-
        ///                     Type cannot be cast automatically to the type of the destination <paramref name="array"/>.
        ///                 </exception>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array", "array is null");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "index < 0");
            }
            _valueLock.EnterReadLock();
            try
            {
                if (Count > (array.Length - arrayIndex))
                {
                    throw new ArgumentOutOfRangeException("array", "not enough room to copy");
                }
                CopyElementsToArray(array, arrayIndex);
            }
            finally
            {
                _valueLock.ExitReadLock();
            }
        }

        private void CopyElementsToArray(TValue[] destArray, int destStartIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                destArray[destStartIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        ///                 </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        ///                 </exception>
        public bool Remove(TValue item)
        {
            int index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///                 </param>
        public int IndexOf(TValue item)
        {
            int index;
            for (index = 0; index < Count; index++)
            {
                if (!this[index].Equals(item))
                {
                    continue;
                }
                return index;
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.
        ///                 </param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///                 </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///                 </exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        ///                 </exception>
        public void Insert(int index, TValue item)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index", "invalid index");
            }
            Add(new TValue()); // make room for one more
            for (int i = Count - 1; i > index; i--)
            {
                this[i] = this[i - 1];
            }
            this[index] = item;
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.
        ///                 </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
        ///                 </exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
        ///                 </exception>
        public void RemoveAt(int index)
        {
            if ((index + 1) == Count)
            {
                Count--;
                return;
            }

            for (int i = index; i < Count - 1; i++)
            {
                this[i] = this[i + 1];
            }
            Count--;
        }

        public TValue this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                }
                try
                {
                    _valueLock.EnterReadLock();
                    return ValueSerializer.BytesToObject(Read(index));
                }
                finally
                {
                    _valueLock.ExitReadLock();
                }
            }
            set
            {
                if (index < 0)
                {
                    throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                }
                try
                {
                    _valueLock.EnterWriteLock();
                    if (index >= Count)
                    {
                        if (AutoGrow)
                        {
                            ViewManager.Grow(index);
                        }
                        else
                        {
                            throw new IndexOutOfRangeException("Tried to access item outside the array boundaries");
                        }
                    }
                    Write(ValueSerializer.ObjectToBytes(value), index);
                }
                finally
                {
                    _valueLock.ExitWriteLock();
                }
            }
        }
        #endregion
    }
}