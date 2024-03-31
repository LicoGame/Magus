using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Serialization;
using MemoryPack;

namespace Magus.Json.Test;

[MemoryPackable, MagusTable(nameof(SimpleClass01))]
public partial class SimpleClass01(int id, string name) : IEquatable<SimpleClass01>
{
    [PrimaryKey] public int Id { get; } = id;
    public string Name { get; } = name;

    public bool Equals(SimpleClass01? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SimpleClass01)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}

public class Simple
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        // Data
        var expectedData = new SimpleClass01[]
        {
            new(1, "name1"),
            new(2, "name2")
        };

        // Schema
        var expectedJson = 
            """
            [
                {
                    "Id": 1,
                    "Name": "name1"
                },
                {
                    "Id": 2,
                    "Name": "name2"
                }
            ]
            """;
        
        var memoryStream = new MemoryStream();
        JsonSchemaGenerator.GenerateArray<SimpleClass01>(memoryStream);
        var schemaText = Encoding.UTF8.GetString(memoryStream.ToArray());
        var schema = JsonSchema.FromText(schemaText);
        
        Console.WriteLine(schemaText);
        
        // Serialization
        ValidatingJsonConverter.MapType<SimpleClass01[]>(schema);
        var options = new JsonSerializerOptions
        {
            Converters = { new ValidatingJsonConverter() }
        };
        var jsonText = JsonSerializer.Serialize(expectedData, options);

        // Evaluation
        var node = JsonNode.Parse(expectedJson);
        var result = schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.That(result.IsValid, Is.True);
        
        // Deserialization
        var actualData = JsonSerializer.Deserialize<SimpleClass01[]>(jsonText, options)!;
        Assert.That(actualData, Is.EquivalentTo(expectedData));

        Assert.Pass();
    }
}