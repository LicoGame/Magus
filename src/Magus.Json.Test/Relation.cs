using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Humanizer;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using Json.Schema.Serialization;
using MemoryPack;

namespace Magus.Json.Test;

[MemoryPackable, MagusTable(nameof(RelationA))]
public partial class RelationA(int id, string name) : IEquatable<RelationA>, IComparable<RelationA>
{
    [PrimaryKey] public int Id { get; set; } = id;

    public string Name { get; set; } = name;

    public bool Equals(RelationA? other)
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
        return Equals((RelationA)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }

    public int CompareTo(RelationA? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var idComparison = Id.CompareTo(other.Id);
        if (idComparison != 0) return idComparison;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

[MemoryPackable, MagusTable(nameof(RelationB))]
public partial class RelationB : IEquatable<RelationB>
{
    public RelationB(int id, int relationAId)
    {
        Id = id;
        RelationAId = relationAId;
    }

    [PrimaryKey] public int Id { get; set; }

    [Relation(nameof(RelationA), nameof(RelationA.Id))]
    public int RelationAId { get; set; }

    public bool Equals(RelationB? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && RelationAId == other.RelationAId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RelationB)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, RelationAId);
    }
}

[TestFixture]
public class Relation
{
    [Test]
    public void RelationField()
    {
        // Data
        var expectedData = new RelationB[]
        {
            new(1, 1),
            new(2, 2)
        };

        // Expected Data Json
        var expectedJson =
            """
            [
                {
                    "id": 1,
                    "relationAId": 1
                },
                {
                    "id": 2,
                    "relationAId": 2
                }
            ]
            """;

        // Expected Schema
        var expectedSchema = new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft7Id)
            .Type(SchemaValueType.Array)
            .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("id", new JsonSchemaBuilder().Type(SchemaValueType.Integer).UniqueItems(true)),
                    ("relationAId", new JsonSchemaBuilder().Type(SchemaValueType.Integer))
                )
            )
            .PrimaryKey("id")
            .Relations(new SchemaHelper.RelationInfo("relationAId",
                nameof(RelationA).Camelize(),
                nameof(RelationA.Id).Camelize())
            )
            .Build();

        var memoryStream = new MemoryStream();
        JsonSchemaGenerator.GenerateArray<RelationB>(memoryStream);
        var schemaText = Encoding.UTF8.GetString(memoryStream.ToArray());
        var schema = JsonSchema.FromText(schemaText);
        
        Assert.That(schemaText, Is.EqualTo(expectedSchema.ToJsonString()));
        
        Console.WriteLine(schemaText);
        
        // Serialization
        ValidatingJsonConverter.MapType<RelationB[]>(schema);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new ValidatingJsonConverter() }
        };
        var jsonText = JsonSerializer.Serialize(expectedData, options);
        
        // Evaluation
        var node = JsonNode.Parse(expectedJson);
        var result = schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.That(result.IsValid, Is.True);
        
        // Deserialization
        var actualData = JsonSerializer.Deserialize<RelationB[]>(jsonText, options);
        Assert.That(actualData, Is.EquivalentTo(expectedData));
    }
}