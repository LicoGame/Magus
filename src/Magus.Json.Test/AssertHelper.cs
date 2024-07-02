using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Magus.Json.Test;

public static class AssertHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToJsonString(this JsonSchema schema)
    {
        var writerOptions = new JsonWriterOptions { Indented = true };
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        var converter = new SchemaJsonConverter();
        var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, writerOptions))
        {
            converter.Write(writer, schema, serializerOptions);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    
    public static void AssertJsonText(string actual, string expected)
    {
        var actualNode = JsonNode.Parse(actual);
        var expectedNode = JsonNode.Parse(expected);
        Assert.That(JsonNode.DeepEquals(actualNode, expectedNode), Is.True);
    }
}