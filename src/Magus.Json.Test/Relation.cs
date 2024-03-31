using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using MemoryPack;

namespace Magus.Json.Test;

//TODO Magusに移す
public class RelationAttribute : Attribute, IAttributeHandler
{
    public string TableName { get; }
    public string FieldName { get; }

    public RelationAttribute(string tableName, string fieldName)
    {
        TableName = tableName;
        FieldName = fieldName;
    }

    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        var self = (RelationAttribute) attribute;
        context.Intents.Add(new RelationIntent(self.TableName, self.FieldName));
    }
}

[MemoryPackable, MagusTable(nameof(RelationA))]
public partial class RelationA(int id, string name) : IEquatable<RelationA>, IComparable<RelationA>
{
    [PrimaryKey]
    public int Id { get; set; } = id;

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
public partial class RelationB
{
    public RelationB(int id, int relationAId)
    {
        Id = id;
        RelationAId = relationAId;
    }

    [PrimaryKey]
    public int Id { get; set; }
    
    [Relation(nameof(RelationA), nameof(RelationA.Id))]
    public int RelationAId { get; set; }
}

public class RelationIntent : ISchemaKeywordIntent
{
    private readonly string _tableName;
    private readonly string _fieldName;

    public RelationIntent(string tableName, string fieldName)
    {
        _tableName = tableName;
        _fieldName = fieldName;
    }
    
    public void Apply(JsonSchemaBuilder builder)
    {
        var obj = new JsonObject();
        obj.Add("table", _tableName);
        obj.Add("field", _fieldName);
        builder.AdditionalProperties(new JsonSchemaBuilder().Unrecognized("relation", obj));
    }
}

[TestFixture]
public class Relation
{
    [Test]
    public void RelationField()
    {
        var memoryStream = new MemoryStream();
        JsonSchemaGenerator.GenerateArray<RelationB>(memoryStream);
        var schemaText = Encoding.UTF8.GetString(memoryStream.ToArray());
        var schema = JsonSchema.FromText(schemaText);
        Console.WriteLine(schemaText);
        Assert.Pass();
    }
}