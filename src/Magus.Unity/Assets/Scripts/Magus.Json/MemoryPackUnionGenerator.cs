using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using MemoryPack;

namespace Magus.Json
{
    public sealed class MemoryPackUnionGenerator : ISchemaGenerator
    {
        public bool Handles(Type type)
        {
            if (!type.IsInterface) return false;
            var attributes = MemoryPackUnionAttributes(type);

            return attributes.Count > 0;
        }

        private static List<MemoryPackUnionAttribute> MemoryPackUnionAttributes(Type type)
        {
            var attributes =
                type.GetCustomAttributes<MemoryPackUnionAttribute>()
                    .OrderBy(attr => attr.Tag)
                    .ToList();
            return attributes;
        }

        public void AddConstraints(SchemaGenerationContextBase context)
        {
            var oneOf = new OneOfIntent();
            var attributes = MemoryPackUnionAttributes(context.Type);
            foreach (var attribute in attributes)
            {
                var ctx = SchemaGenerationContextCache.Get(attribute.Type);
                oneOf.Subschemas.Add(ctx.Intents);
            }

            context.Intents.Add(oneOf);
        }
    }
}