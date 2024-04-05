using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Json.Schema.Generation;

namespace Magus.Json
{
    public static class InternalBridge
    {
        private static Dictionary<int, SchemaGenerationContextBase>? _cache;
        private static Func<Type, IEnumerable<Attribute>?, int>? _calculateHash;

        // TODO: Replace with UnsafeAccessor if implemented for Static class
        public static Dictionary<int, SchemaGenerationContextBase> GetCache()
        {
            if (_cache != null)
            {
                return _cache;
            }

            // Get cache of static property with Reflection from SchemaGenerationContextCache
            var t = typeof(SchemaGenerationContextCache);
            var property = t.GetProperty("Cache",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return _cache = (Dictionary<int, SchemaGenerationContextBase>)property!.GetValue(null)!;
        }
        
        public static int CalculateHash(Type type, IEnumerable<Attribute>? attributes)
        {
            if (_calculateHash == null)
            {
                var t = typeof(SchemaGenerationContextCache);
                var property = t.GetProperty("CalculateHash",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                _calculateHash = (Func<Type, IEnumerable<Attribute>?, int>)property!.GetValue(null)!;
            }
            return _calculateHash.Invoke(type, attributes).GetHashCode();
        }

        public static void ReplaceCache(SchemaGenerationContextBase previous, SchemaGenerationContextBase current)
        {
            var cache = GetCache();
            cache.Remove(previous.Hash);
            // NOTE: Compute Hash using all Attributes
            current.Hash = CalculateHash(current.Type, current.GetAttributes());
            cache[current.Hash] = current;
        }
    }
}