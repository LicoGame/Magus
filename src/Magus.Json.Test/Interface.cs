using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Generator.Equals;
using Json.Schema;
using Json.Schema.Serialization;
using MemoryPack;

namespace Magus.Json.Test;

[MemoryPackable]
[MemoryPackUnion(0, typeof(HpConsumer))]
[MemoryPackUnion(1, typeof(MpConsumer))]
public partial interface IConsumer
{
}

[MemoryPackable]
[Equatable]
public partial class HpConsumer : IConsumer
{
    public int Hp { get; set; }
}

[MemoryPackable]
[Equatable]
public partial class MpConsumer : IConsumer
{
    public int Mp { get; set; }
}

class ConsumerInterfaceComparer : IEqualityComparer<IConsumer>
{
    public bool Equals(IConsumer? x, IConsumer? y)
    {
        return (x, y) switch
        {
            (HpConsumer a, HpConsumer b) => a.Hp == b.Hp,
            (MpConsumer a, MpConsumer b) => a.Mp == b.Mp,
            _ => false
        };
    }

    public int GetHashCode(IConsumer obj)
    {
        return obj switch
        {
            HpConsumer a => a.GetHashCode(),
            MpConsumer a => a.GetHashCode(),
            _ => 0
        };
    }
}

class ConsumersInterfaceComparer : IEqualityComparer<IConsumer[]>
{
    public bool Equals(IConsumer[]? x, IConsumer[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.Length != y.Length) return false;
        var comparer = new ConsumerInterfaceComparer();
        for (int i = 0; i < x.Length; i++)
        {
            if (!comparer.Equals(x[i], y[i])) return false;
        }
        return true;
    }

    public int GetHashCode(IConsumer[] obj)
    {
        var hashCode = new global::System.HashCode();
        foreach (var item in obj)
        {
            hashCode.Add(item);
        }
        return hashCode.ToHashCode();
    }
}

[MemoryPackable, MagusTable(nameof(ConsumerData))]
[Equatable]
public partial class ConsumerData
{
    public ConsumerData(int id, IConsumer[] consumers)
    {
        Id = id;
        Consumers = consumers;
    }

    [PrimaryKey] public int Id { get; set; }

    [CustomEquality(typeof(ConsumersInterfaceComparer))]
    public IConsumer[] Consumers { get; set; }
}

public class InterfaceTest
{
    [Test]
    public void PolymorphicInterfaceTest()
    {
        // Data
        var expectedData = new ConsumerData[]
        {
            new(1,
                new IConsumer[] { new HpConsumer { Hp = 10 }, new MpConsumer { Mp = 20 } })
        };

        // Expected Data Json
        var expectedJson =
            """
            [{
                "id": 1,
                "consumers": [
                    {
                        "$tag": 0,
                        "hp": 10
                    },
                    {
                        "$tag": 1,
                        "mp": 20
                    }
                ]
            }]
            """;

        // Expected Schema
        var expectedSchema = new JsonSchemaBuilder()
            .Schema(MagusMetaSchemas.RelationExtId)
            .Type(SchemaValueType.Array)
            .Items(new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("id", new JsonSchemaBuilder().Type(SchemaValueType.Integer).UniqueItems(true)),
                    ("consumers", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                        .Items(new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .OneOf(
                                new JsonSchemaBuilder()
                                    .Type(SchemaValueType.Object)
                                    .Properties(
                                        ("hp", new JsonSchemaBuilder().Type(SchemaValueType.Integer)),
                                        ("$tag", new JsonSchemaBuilder().Const(JsonValue.Create(0)))
                                    )
                                    .AdditionalProperties(false),
                                new JsonSchemaBuilder()
                                    .Type(SchemaValueType.Object)
                                    .Properties(
                                        ("mp", new JsonSchemaBuilder().Type(SchemaValueType.Integer)),
                                        ("$tag", new JsonSchemaBuilder().Const(JsonValue.Create(1)))
                                    )
                                    .AdditionalProperties(false))
                        )
                    ))
                .AdditionalProperties(false))
            .PrimaryKey("id")
            .Build();

        MagusVocabularies.Register();
        
        var memoryStream = new MemoryStream();
        MagusJsonSchema.GenerateArray<ConsumerData>(memoryStream);
        var schemaText = Encoding.UTF8.GetString(memoryStream.ToArray());
        var schema = JsonSchema.FromText(schemaText);

        Console.WriteLine(schemaText);
        Console.WriteLine(expectedSchema.ToJsonString());
        Assert.That(schemaText, Is.EqualTo(expectedSchema.ToJsonString()));


        // Serialization
        ValidatingJsonConverter.MapType<ConsumerData[]>(schema);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new MemoryPackUnionConverter(), new ValidatingJsonConverter
            {
                OutputFormat = OutputFormat.Hierarchical,
                RequireFormatValidation = true
            } },
            WriteIndented = true
        };

        var jsonText = JsonSerializer.Serialize(expectedData, options);
        AssertHelper.AssertJsonText(jsonText, expectedJson);

        // Evaluation
        var evaluateOptions = new EvaluationOptions
            { OutputFormat = OutputFormat.Hierarchical };
        var node = JsonNode.Parse(expectedJson);
        var result = schema.Evaluate(node, evaluateOptions);
        if (result.IsValid == false)
        {
            Console.WriteLine(result.ToJsonString());
        }

        Assert.That(result.IsValid, Is.True);

        try
        {
            var resultData = JsonSerializer.Deserialize<ConsumerData[]>(jsonText, options);
            Assert.That(resultData, Is.EquivalentTo(expectedData).Using<ConsumerData>(EqualityComparer<ConsumerData>.Default));
        }
        catch (JsonException e)
        {
            var validation = e.Data["validation"] as EvaluationResults;
            Console.WriteLine(validation?.ToJsonString());
            Assert.Fail();
        }
    }
}