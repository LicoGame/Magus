using Json.Schema;
using Magus.Json.Keywords;

namespace Magus.Json
{
    public static class MagusVocabularies
    {
        public const string RelationExtId =
            "https://licogame.github.io/Magus.Json.MetaSchema/schema/vocabs/relation-ext";
        
        public static readonly Vocabulary RelationExt = new Vocabulary(RelationExtId, 
            typeof(PrimaryKeyKeyword),
            typeof(RelationsKeyword)
            );

        public static void Register(VocabularyRegistry? vocabularyRegistry = null, SchemaRegistry? schemaRegistry = null)
        {
            schemaRegistry ??= SchemaRegistry.Global;
            vocabularyRegistry ??= VocabularyRegistry.Global;
            
            vocabularyRegistry.Register(RelationExt);
            SchemaKeywordRegistry.Register<PrimaryKeyKeyword>(JsonSchemaRelationExtSerializerContext.Default);
            SchemaKeywordRegistry.Register<RelationsKeyword>(JsonSchemaRelationExtSerializerContext.Default);
            schemaRegistry.Register(MagusMetaSchemas.RelationExt);
            schemaRegistry.Register(MagusMetaSchemas.RelationExt202012);
        }
    }
}