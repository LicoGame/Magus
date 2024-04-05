using System;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace Magus.Json
{
    internal class PrimaryKeyAttributeHandler : IAttributeHandler<PrimaryKeyAttribute>
    {
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
        {
            context.Intents.Add(new UniqueItemsIntent(true));
        }
    }
}