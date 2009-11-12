using System.Collections.Generic;

namespace mAdcOW.DataStructures
{
    public interface IDictionaryPersist<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        int Count { get; }
        bool ContainsKey(TKey key);
        bool ContainsValue(TValue value);
        void Add(TKey key, TValue value);
        bool Remove(TKey key);
        bool TryGetValue(TKey key, out TValue value);
        bool ByteCompare(TValue value, TValue existing);
        ICollection<TKey> AllKeys();
        ICollection<TValue> AllValues();
        void Clear();
    }
}