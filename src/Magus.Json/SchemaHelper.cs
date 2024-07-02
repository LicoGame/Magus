using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Json.Schema;
using Magus.Json.Keywords;

namespace Magus.Json
{
    public static class SchemaHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonSchemaBuilder PrimaryKey(this JsonSchemaBuilder builder, string fieldName)
        {
            builder.Add(new PrimaryKeyKeyword(fieldName));
            return builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonSchemaBuilder Relations(this JsonSchemaBuilder builder, params RelationsKeyword.RelationInfo[] relations)
        {
            builder.Add(new RelationsKeyword(relations));
            return builder;
            // return builder.Unrecognized("relations", new JsonArray(
            //         relations.Select(x =>
            //         {
            //             JsonNode node = new JsonObject
            //             {
            //                 { "from", x.FromFieldName },
            //                 {
            //                     "to", new JsonObject
            //                     {
            //                         { "table", x.ToTableName },
            //                         { "field", x.ToFieldName }
            //                     }
            //                 }
            //             };
            //             return node;
            //         }).ToArray()
            //     )
            // );
        }
    }
}