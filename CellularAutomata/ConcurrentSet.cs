using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomata
{
    internal class ConcurrentSet<T> : ISet<T> where T : notnull
    {
        public ConcurrentSet() { }
        public ConcurrentSet(IEnumerable<T> other)
        {
            Random random = new();
            foreach (var item in other)
            {
                int rnd = random.Next();
                _ConcurrentDictionary.GetOrAdd(item, rnd);
            }
        }

        private readonly ConcurrentDictionary<T, int> _ConcurrentDictionary = [];

        int ICollection<T>.Count => _ConcurrentDictionary.Count;

        bool ICollection<T>.IsReadOnly => false;

        public bool Add(T item)
        {
            int rnd = Random.Shared.Next();

            var value = _ConcurrentDictionary.GetOrAdd(item, rnd);

            return rnd == value;
        }

        void ICollection<T>.Add(T item) => ((ISet<T>)this).Add(item);

        void ICollection<T>.Clear() => _ConcurrentDictionary.Clear();

        bool ICollection<T>.Contains(T item) => _ConcurrentDictionary.ContainsKey(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _ConcurrentDictionary.Keys.CopyTo(array, arrayIndex);

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                _ConcurrentDictionary.Remove(item, out var _);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _ConcurrentDictionary.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _ConcurrentDictionary.Keys.GetEnumerator();

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();

            foreach (var item in _ConcurrentDictionary.Keys)
            {
                if (!otherSet.Contains(item))
                {
                    _ConcurrentDictionary.Remove(item, out var _);
                }
            }
        }

        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();
            if (_ConcurrentDictionary.Keys.Count >= otherSet.Count)
                return false;
            foreach (var item in _ConcurrentDictionary.Keys)
            {
                if (!otherSet.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();
            if (_ConcurrentDictionary.Keys.Count <= otherSet.Count)
                return false;
            foreach (var item in otherSet)
            {
                if (!_ConcurrentDictionary.ContainsKey(item))
                {
                    return false;
                }
            }

            return true;
        }

        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();
            if (_ConcurrentDictionary.Keys.Count > otherSet.Count)
                return false;
            foreach (var item in _ConcurrentDictionary.Keys)
            {
                if (!otherSet.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();
            if (_ConcurrentDictionary.Keys.Count < otherSet.Count)
                return false;
            foreach (var item in otherSet)
            {
                if (!_ConcurrentDictionary.ContainsKey(item))
                {
                    return false;
                }
            }

            return true;
        }

        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                if (_ConcurrentDictionary.ContainsKey(item))
                {
                    return true;
                }
            }
            return false;
        }

        bool ICollection<T>.Remove(T item)
        {
            return _ConcurrentDictionary.Remove(item, out var _);
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                if (!_ConcurrentDictionary.ContainsKey(item))
                {
                    return false;
                }
            }
            return true;
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            var otherSet = (other as ISet<T>) ?? other.ToHashSet();

            foreach (var item in otherSet)
            {
                if (!_ConcurrentDictionary.Remove(item, out var _))
                {
                    int rnd = Random.Shared.Next();
                    _ConcurrentDictionary.GetOrAdd(item, rnd);
                }
            }
        }

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                int rnd = Random.Shared.Next();
                _ConcurrentDictionary.GetOrAdd(item, rnd);
            }
        }
    }
}
