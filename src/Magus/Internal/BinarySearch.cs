using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Magus.Core.Internal
{
    internal static class BinarySearch
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindFirst<T, TKey>(T[] array, TKey key, Func<T, TKey> selector, IComparer<TKey> comparer)
        {
            var low = 0;
            var high = array.Length - 1;
            
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var cmp = comparer.Compare(selector(array[mid]), key);
                
                if (cmp < 0)
                    low = mid + 1;
                else if (cmp > 0)
                    high = mid - 1;
                else
                    return mid;
            }
            
            return -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindFirstIntKeys<T>(T[] array, int key, Func<T, int> selector)
        {
            var low = 0;
            var high = array.Length - 1;
            
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var value = selector(array[mid]);
                var cmp = (value < key) ? -1 : (value > key) ? 1 : 0;
                
                if (cmp < 0)
                    low = mid + 1;
                else if (cmp > 0)
                    high = mid - 1;
                else
                    return mid;
            }
            
            return -1;
        }

        /// <summary>
        /// 指定されたキーに最も近い要素のインデックスを与えられた配列から検索します。
        /// </summary>
        /// <param name="array">検索する要素のソート済み配列。</param>
        /// <param name="key">要素と比較するキー。</param>
        /// <param name="selector">要素からキーを選択するための関数。</param>
        /// <param name="comparer">キーを比較するために使用される比較子。</param>
        /// <param name="selectLower">インデックスが同じである場合に、より小さいインデックスを選択するかどうかを示すフラグ。</param>
        /// <returns>指定されたキーに最も近い要素のインデックス。配列が空の場合、-1が返されます。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindClosest<T, TKey>(T[] array, TKey key, Func<T, TKey> selector, IComparer<TKey> comparer, bool selectLower)
        {
            if (array.Length == 0) return -1;
            
            var low = -1;
            var high = array.Length;
            
            while (high - low > 1)
            {
                var mid = low + ((high - low) >> 1);
                var cmp = comparer.Compare(selector(array[mid]), key);
                
                if (cmp < 0)
                    low = mid;
                else if (cmp > 0)
                    high = mid;
                else
                {
                    low = high = mid;
                    break;
                }
            }
            
            return selectLower ? low : high;
        }
        
        /// <summary>
        /// ソート済み配列内で指定されたキーの下限を探索します。<br/>
        /// </summary>
        /// <param name="array">検索対象のソート済み配列。</param>
        /// <param name="key">検索するキー。</param>
        /// <param name="selector">要素からキーを選択するための関数。</param>
        /// <param name="comparer">キーを比較するために使用される比較子。</param>
        /// <returns>見つかった場合は指定されたキーよりも大きいか等しい最初の要素のインデックス。それ以外の場合は-1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LowerBound<T, TKey>(T[] array, TKey key, Func<T, TKey> selector, IComparer<TKey> comparer)
        {
            var low = 0;
            var high = array.Length;
            
            while (low < high)
            {
                var mid = low + ((high - low) >> 1);
                var cmp = comparer.Compare(selector(array[mid]), key);
                
                if (cmp <= 0)
                    high = mid;
                else
                    low = mid + 1;
            }

            var index = low;
            if (index < 0 || index >= array.Length)
                return -1;
            
            return (comparer.Compare(key, selector(array[index])) == 0) ? index : -1;
        }
        
        /// <summary>
        /// 二分探索を使用して、ソートされた配列内のキーの上限を探索します。<br/>
        /// </summary>
        /// <param name="array">探索するソートされた配列</param>
        /// <param name="key">探索するキー</param>
        /// <param name="selector">要素からキーを選択するための関数。</param>
        /// <param name="comparer">キーを比較するために使用される比較子。</param>
        /// <returns>見つかった場合は指定されたキーよりも大きい最初の要素のインデックスを返します。それ以外の場合は-1。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpperBound<T, TKey>(T[] array, TKey key, Func<T, TKey> selector, IComparer<TKey> comparer)
        {
            var low = 0;
            var high = array.Length;
            
            while (low < high)
            {
                var mid = low + ((high - low) >> 1);
                var cmp = comparer.Compare(selector(array[mid]), key);
                
                if (cmp >= 0)
                    low = mid + 1;
                else
                    high = mid;
            }
            
            var index = (low == 0) ? 0 : low - 1;
            if (index < 0 || index >= array.Length)
                return -1;
            
            return (comparer.Compare(key, selector(array[index])) == 0) ? index : -1;
        }
    }
}