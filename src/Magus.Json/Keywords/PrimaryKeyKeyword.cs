using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;

namespace Magus.Json.Keywords
{
    [SchemaKeyword(Name)]
    [SchemaSpecVersion(SpecVersion.Draft201909)]
    [SchemaSpecVersion(SpecVersion.Draft202012)]
    [SchemaSpecVersion(SpecVersion.DraftNext)]
    [Vocabulary(MagusVocabularies.RelationExtId)]
    [JsonConverter(typeof(PrimaryKeyKeywordJsonConverter))]
    public class PrimaryKeyKeyword : IJsonSchemaKeyword
    {
        public PrimaryKeyKeyword(string field)
        {
            Field = field;
        }

        public const string Name = "primaryKey";
        public const string FieldName = "field";

        public string Field { get; }

        public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint,
            IReadOnlyList<KeywordConstraint> localConstraints,
            EvaluationContext context)
        {
            return new KeywordConstraint(Name, Evaluator);
        }

        private void Evaluator(KeywordEvaluation evaluation, EvaluationContext context)
        {
            return;
            if (evaluation.LocalInstance is not JsonArray array) return;

            if (array.Count == 0) return;

            foreach (var jsonNode in array)
            {
                var found = false;
                if (jsonNode is not JsonObject obj) continue;
                foreach (var (key, value) in obj)
                {
                    if (key.ToLower() != Field) continue;

                    found = true;
                    break;
                }
                if (!found)
                {
                    evaluation.Results.Fail(Name, $"Missing PrimaryKey field [[field]] at [[object]]"
                        .ReplaceToken("[[field]]", Field)
                        .ReplaceToken("[[object]]", obj, JsonSchemaRelationExtSerializerInternalContext.Default.JsonObject));
                    continue;
                }
                if (obj[Field] is not JsonValue _) continue;
            }
        }
    }

    public sealed class PrimaryKeyKeywordJsonConverter : WeaklyTypedJsonConverter<PrimaryKeyKeyword>
    {
        public override PrimaryKeyKeyword? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected object");
            reader.Read();
            string? field = null; 
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName");

                var key = reader.GetString();
                if (key != PrimaryKeyKeyword.FieldName)
                    throw new JsonException("Expected PropertyName as " + PrimaryKeyKeyword.FieldName);
                reader.Read();
                field = reader.GetString();
                reader.Read();
            }
            
            if (field == null)
                throw new JsonException("Expected field");
            return new PrimaryKeyKeyword(field!);
        }

        public override void Write(Utf8JsonWriter writer, PrimaryKeyKeyword value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(PrimaryKeyKeyword.FieldName);
            writer.WriteStringValue(value.Field);
            writer.WriteEndObject();
        }
    }
}