using System;
using System.Collections.Generic;
using System.Text.Json;
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
    [JsonConverter(typeof(RelationsKeywordJsonConverter))]
    public class RelationsKeyword : IJsonSchemaKeyword
    {
        public const string Name = "relations";
        public const string FromName = "from";
        public const string ToName = "to";
        public const string FieldName = "field";
        public const string TableName = "table";

        public struct RelationInfo
        {
            public string FromField { get; set; }
            public string ToTable { get; set; }
            public string ToField { get; set; }

            public RelationInfo(string fromField, string toTable, string toField)
            {
                this.FromField = fromField;
                this.ToTable = toTable;
                this.ToField = toField;
            }
        }

        public RelationInfo[] Relations { get; }

        public RelationsKeyword(params RelationInfo[] relations)
        {
            Relations = relations;
        }

        public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint,
            IReadOnlyList<KeywordConstraint> localConstraints,
            EvaluationContext context)
        {
            return new KeywordConstraint(Name, Evaluator);
        }

        private void Evaluator(KeywordEvaluation evaluation, EvaluationContext context)
        {
            // TODO
            return;
        }
    }

    public sealed class RelationsKeywordJsonConverter : WeaklyTypedJsonConverter<RelationsKeyword>
    {
        public override RelationsKeyword? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected Array");

            var relationElements = options.ReadArray(ref reader,
                JsonSchemaRelationExtSerializerInternalContext.Default.JsonElement);
            
            var relations = new List<RelationsKeyword.RelationInfo>();
            
            foreach (var relationElement in relationElements ?? Array.Empty<JsonElement>())
            {
                var fromField = relationElement.GetProperty(RelationsKeyword.FromName).GetProperty(RelationsKeyword.FieldName).GetString();
                var toTable = relationElement.GetProperty(RelationsKeyword.ToName).GetProperty(RelationsKeyword.TableName).GetString();
                var toField = relationElement.GetProperty(RelationsKeyword.ToName).GetProperty(RelationsKeyword.FieldName).GetString();
                relations.Add(new RelationsKeyword.RelationInfo(fromField!, toTable!, toField!));
            }
            
            return new RelationsKeyword(relations.ToArray());
        }

        public override void Write(Utf8JsonWriter writer, RelationsKeyword value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var relation in value.Relations)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(RelationsKeyword.FromName);
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(RelationsKeyword.FieldName);
                    writer.WriteStringValue(options.PropertyNamingPolicy?.ConvertName(relation.FromField) ?? relation.FromField);
                    writer.WriteEndObject();
                }
                writer.WritePropertyName(RelationsKeyword.ToName);
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(RelationsKeyword.TableName);
                    writer.WriteStringValue(options.PropertyNamingPolicy?.ConvertName(relation.ToTable) ?? relation.ToTable);
                    writer.WritePropertyName(RelationsKeyword.FieldName);
                    writer.WriteStringValue(options.PropertyNamingPolicy?.ConvertName(relation.ToField) ?? relation.ToField);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}