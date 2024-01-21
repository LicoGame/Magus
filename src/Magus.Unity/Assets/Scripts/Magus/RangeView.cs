using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Magus
{
    public struct RangeView<T> : IEnumerable<T>, IReadOnlyList<T>, IList<T>
    {
        public static RangeView<T> Empty => new RangeView<T>(null, -1, -1);
        private readonly T[]? _orderedValues;
        private readonly int _left;
        private readonly int _right;
        private readonly bool _ascending;
        private readonly bool _hasValue;
        public int Count => !_hasValue ? 0 : _right - _left + 1;
        public bool IsReadOnly => true;
        public T First => this[0];
        public T Last => this[Count - 1];
        public RangeView<T> Reverse => new RangeView<T>(_orderedValues, _right, _left, !_ascending);

        public RangeView(T[]? orderedValues, int left, int right, bool ascending = true)
        {
            _hasValue = (orderedValues != null) && (orderedValues.Length > 0) && (left <= right);
            _orderedValues = orderedValues;
            _left = left;
            _right = right;
            _ascending = ascending;
        }

        public T this[int index]
        {
            get
            {
                if (!_hasValue) throw new ArgumentOutOfRangeException(nameof(_hasValue), "view is empty");
                if ((index < 0) || (index >= Count)) throw new ArgumentOutOfRangeException(nameof(index));
                if (_ascending)
                    return _orderedValues![_left + index];
                else
                    return _orderedValues![_right - index];
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            var count = Count;
            for (var i = 0; i < count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Any() => Count > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item) => Array.IndexOf(_orderedValues!, item, _left, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            var count = Count;
            Array.Copy(_orderedValues!, _left, array, arrayIndex, count);
            if (!_ascending) Array.Reverse(array, arrayIndex, count);
        }
        
        T IList<T>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }
        
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
        
        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        
        void ICollection<T>.Clear() => throw new NotSupportedException();
        
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}