using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Magus.Json
{
    public static class SchemaHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonSchemaBuilder PrimaryKey(this JsonSchemaBuilder builder, string fieldName)
        {
            return builder.Unrecognized("primaryKey", new JsonObject { { "field", fieldName } });
        }

        public readonly struct RelationInfo
        {
            public RelationInfo(string fromFieldName, string toTableName, string toFieldName)
            {
                FromFieldName = fromFieldName;
                ToTableName = toTableName;
                ToFieldName = toFieldName;
            }

            public readonly string FromFieldName;
            public readonly string ToTableName;
            public readonly string ToFieldName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsonSchemaBuilder Relations(this JsonSchemaBuilder builder, params RelationInfo[] relations)
        {
            return builder.Unrecognized("relations", new JsonArray(
                    relations.Select(x =>
                    {
                        JsonNode node = new JsonObject
                        {
                            { "from", x.FromFieldName },
                            {
                                "to", new JsonObject
                                {
                                    { "table", x.ToTableName },
                                    { "field", x.ToFieldName }
                                }
                            }
                        };
                        return node;
                    }).ToArray()
                )
            );
        }
    }
}