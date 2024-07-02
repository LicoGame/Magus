using System;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using MemoryPack;

namespace Magus.Json
{
    public class MemoryPackableAttributeHandler : IAttributeHandler<MemoryPackableAttribute>
    {
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
        {
            if (context.Type.IsInterface) return;
            context.Intents.Add(new AdditionalPropertiesIntent(SchemaGenerationContextBase.False));
        }
    }
}