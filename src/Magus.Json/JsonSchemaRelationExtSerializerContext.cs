using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;
using Json.Schema;
using Magus.Json.Keywords;

namespace Magus.Json
{
    [JsonSerializable(typeof(RelationsKeyword))]
    [JsonSerializable(typeof(PrimaryKeyKeyword))]
    internal partial class JsonSchemaRelationExtSerializerContext : JsonSerializerContext
    {
    };
    
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(JsonElement))]
    internal partial class JsonSchemaRelationExtSerializerInternalContext : JsonSerializerContext
    {}
}