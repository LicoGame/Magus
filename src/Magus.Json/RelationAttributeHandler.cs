using System;
using Json.Schema.Generation;
using MemoryPack.Internal;

namespace Magus.Json
{
    internal class RelationAttributeHandler : IAttributeHandler<RelationAttribute>
    {
        [Preserve]
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
        {
            // NOTE: Handle by MagusRefiner, do nothing here
        }
    }
}