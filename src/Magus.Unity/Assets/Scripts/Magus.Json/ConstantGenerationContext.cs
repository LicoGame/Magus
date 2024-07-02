using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace Magus.Json
{
    public sealed class ConstantGenerationContext<T> : SchemaGenerationContextBase
    {
        internal ConstantGenerationContext(T value) : base(typeof(T))
        {
            Intents.Add(new ConstIntent(JsonValue.Create(value)));
        }
    }

    public static class ConstantGenerationContext
    {
        public static ConstantGenerationContext<int> Int(int value) => new ConstantGenerationContext<int>(value); // <int>
    }
}