using System;
using Json.Schema;

namespace Magus.Json
{
    public static class MagusMetaSchemas
    {
        public static readonly Uri RelationExtId =
            new Uri("https://licogame.github.io/Magus.Json.MetaSchema/meta/relation-ext");

        public static readonly Uri RelationExt202012Id =
            new Uri("https://licogame.github.io/Magus.Json.MetaSchema/meta/vocab/relation-ext");

        public static readonly JsonSchema RelationExt202012 =
            new JsonSchemaBuilder()
                .Schema(MetaSchemas.Draft202012Id)
                .Id(RelationExtId)
                .Vocabulary(
                    (Vocabularies.Core202012Id, true),
                    (Vocabularies.Applicator202012Id, true),
                    (Vocabularies.Validation202012Id, true),
                    (Vocabularies.Metadata202012Id, true),
                    (Vocabularies.FormatAnnotation202012Id, true),
                    (Vocabularies.Content202012Id, true),
                    (Vocabularies.Unevaluated202012Id, true)
                )
                .DynamicAnchor("meta")
                .Title("Relation extensions 2020-12 meta-schema")
                .AllOf(
                    new JsonSchemaBuilder().Ref(MetaSchemas.Draft202012Id),
                    new JsonSchemaBuilder().Ref(RelationExt202012Id))
                .Build();

        public static readonly JsonSchema RelationExt =
            new JsonSchemaBuilder()
                .Schema(MetaSchemas.Draft202012Id)
                .Id(RelationExt202012Id)
                .Title("Relation extensions meta-schema")
                .Properties(
                    (PrimaryKeyKeyword.Name, new JsonSchemaBuilder()
                        .Type(SchemaValueType.String)
                        .Properties((PrimaryKeyKeyword.Field, new JsonSchemaBuilder()
                            .Type(SchemaValueType.String)))),
                    (RelationsKeyword.Name, new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                        .Items(new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(
                                (RelationsKeyword.From, new JsonSchemaBuilder()
                                    .Type(SchemaValueType.String)),
                                (RelationsKeyword.To, new JsonSchemaBuilder()
                                    .Type(SchemaValueType.Object)
                                    .Properties(
                                        (RelationsKeyword.Table, new JsonSchemaBuilder()
                                            .Type(SchemaValueType.String)),
                                        (RelationsKeyword.Field, new JsonSchemaBuilder()
                                            .Type(SchemaValueType.String))
                                    )
                                )
                            )
                        )
                    )
                );

        public static void RegisterSchemas(SchemaRegistry registry)
        {
            registry.Register(RelationExt202012Id, RelationExt202012);
            registry.Register(RelationExtId, RelationExt);
        }
    }
}