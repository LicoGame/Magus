using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Magus.Core;
using Magus.Core.Internal;

namespace Magus
{
    public abstract class TableBase<T>
    {
        protected readonly T[] Values;

        public int Count => Values.Length;
        public RangeView<T> All => new RangeView<T>(Values, 0, Values.Length - 1);
        public RangeView<T> AllReverse => new RangeView<T>(Values, 0, Values.Length - 1, false);
        public Span<T> AllAsSpan => Values;
        public Span<T> AllReverseAsSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var v = Values.AsSpan();
                v.Reverse();
                return v;
            }
        }
        public T[] GetRawDataUnsafe() => Values;
        
        protected TableBase(T[] sortedValues)
        {
            this.Values = sortedValues;
        }

        protected T[] CloneAndSortBy<TKey>(Func<T, TKey> selector, IComparer<TKey> comparer)
        {
            var sortedValues = new T[Values.Length];
            var sortSource = new TKey[Values.Length];
            for (var i = 0; i < Values.Length; i++)
            {
                sortedValues[i] = Values[i];
                sortSource[i] = selector(Values[i]);
            }

            Array.Sort(sortSource, sortedValues, 0, sortedValues.Length, comparer);

            return sortedValues;
        }

        static protected T ThrowKeyNotFountException<TKey>(TKey key) =>
            throw new KeyNotFoundException($"Key: {key} not found in table.");

        static protected T FindUniqueCore<TKey>(T[] array, TKey key, Func<T, TKey> selector, IComparer<TKey> comparer,
            bool throwable = true)
        {
            var index = BinarySearch.FindFirst(array, key, selector, comparer);
            if (index < 0)
            {
                if (throwable)
                    ThrowKeyNotFountException(key);
                else
                    return default!;
            }

            return array[index];
        }

        static protected T FindUniqueCoreInt(T[] array, int key, Func<T, int> selector, bool throwable = true)
        {
            var index = BinarySearch.FindFirstIntKeys(array, key, selector);
            if (index < 0)
            {
                if (throwable)
                    ThrowKeyNotFountException(key);
                else
                    return default!;
            }

            return array[index];
        }

        static protected bool TryFindUniqueCore<TKey>(T[] array, TKey key, Func<T, TKey> selector,
            IComparer<TKey> comparer, out T result)
        {
            var index = BinarySearch.FindFirst(array, key, selector, comparer);
            if (index < 0)
            {
                result = default!;
                return false;
            }

            result = array[index];
            return true;
        }

        static protected bool TryFindUniqueCoreInt(T[] array, int key, Func<T, int> selector, out T result)
        {
            var index = BinarySearch.FindFirstIntKeys(array, key, selector);
            if (index < 0)
            {
                result = default!;
                return false;
            }

            result = array[index];
            return true;
        }

        static protected T FindUniqueClosestCore<TKey>(T[] array, TKey key, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool selectLower)
        {
            var index = BinarySearch.FindClosest(array, key, selector, comparer, selectLower);
            return index != -1 ? array[index] : default!;
        }

        static protected RangeView<T> FindUniqueRangeCore<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool ascending = true)
        {
            var low = BinarySearch.FindClosest(array, min, selector, comparer, false);
            var high = BinarySearch.FindClosest(array, max, selector, comparer, true);

            if (low == -1) low = 0;
            if (high == array.Length) high -= 1;

            return new RangeView<T>(array, low, high, ascending);
        }

        static protected Span<T> FindUniqueRangeCoreAsSpan<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool ascending = true)
        {
            var low = BinarySearch.FindClosest(array, min, selector, comparer, false);
            var high = BinarySearch.FindClosest(array, max, selector, comparer, true);

            if (low == -1) low = 0;
            if (high == array.Length) high -= 1;

            var span = new Span<T>(array, low, high - low + 1);
            if (!ascending)
                span.Reverse();
            
            return span;
        }

        static protected RangeView<T> FindManyCore<TKey>(T[] array, TKey key, Func<T, TKey> selector,
            IComparer<TKey> comparer)
        {
            var low = BinarySearch.LowerBound(array, key, selector, comparer);
            if (low == -1) return RangeView<T>.Empty;

            var high = BinarySearch.UpperBound(array, key, selector, comparer);
            if (high == -1) return RangeView<T>.Empty;

            return new RangeView<T>(array, low, high, true);
        }

        static protected ReadOnlySpan<T> FindManyCoreAsSpan<TKey>(T[] array, TKey key, Func<T, TKey> selector,
            IComparer<TKey> comparer)
        {
            var low = BinarySearch.LowerBound(array, key, selector, comparer);
            if (low == -1) return ReadOnlySpan<T>.Empty;

            var high = BinarySearch.UpperBound(array, key, selector, comparer);
            if (high == -1) return ReadOnlySpan<T>.Empty;

            return new ReadOnlySpan<T>(array, low, high - low + 1);
        }

        static protected RangeView<T> FindManyClosestCore<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool selectLower)
        {
            var closest = BinarySearch.FindClosest(array, min, selector, comparer, selectLower);

            if (closest == -1 || closest >= array.Length) return RangeView<T>.Empty;

            return FindManyCore(array, selector(array[closest]), selector, comparer);
        }

        static protected ReadOnlySpan<T> FindManyClosestCoreAsSpan<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool selectLower)
        {
            var closest = BinarySearch.FindClosest(array, min, selector, comparer, selectLower);

            if (closest == -1 || closest >= array.Length) return ReadOnlySpan<T>.Empty;

            return FindManyCoreAsSpan(array, selector(array[closest]), selector, comparer);
        }

        static protected RangeView<T> FindManyRangeCore<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool ascending = true)
        {
            if (comparer.Compare(min, max) > 0)
                return RangeView<T>.Empty;

            var low = BinarySearch.LowerBound(array, min, selector, comparer);
            var high = BinarySearch.UpperBound(array, max, selector, comparer);

            if (low == -1 || high == -1) return RangeView<T>.Empty;
            if (low > high) return RangeView<T>.Empty;

            return new RangeView<T>(array, low, high, ascending);
        }
        
        static protected ReadOnlySpan<T> FindManyRangeCoreAsSpan<TKey>(T[] array, TKey min, TKey max, Func<T, TKey> selector,
            IComparer<TKey> comparer, bool ascending = true)
        {
            if (comparer.Compare(min, max) > 0)
                return ReadOnlySpan<T>.Empty;
            
            var low = BinarySearch.LowerBound(array, min, selector, comparer);
            var high = BinarySearch.UpperBound(array, max, selector, comparer);
            
            if (low == -1 || high == -1) return ReadOnlySpan<T>.Empty;
            if (low > high) return ReadOnlySpan<T>.Empty;
            
            var span = new Span<T>(array, low, high - low + 1);
            if (!ascending)
                span.Reverse();
            
            return span;
        }
    }
}