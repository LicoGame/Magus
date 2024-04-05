using System;
using Json.Schema.Generation;

namespace Magus.Json
{
    internal class RelationAttributeHandler : IAttributeHandler<RelationAttribute>
    {
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
        {
            // NOTE: Handle by MagusRefiner, do nothing here
        }
    }
}