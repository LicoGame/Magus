using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Humanizer;
using Json.Schema;
using Json.Schema.Generation;

namespace Magus.Json
{
    internal class RelationsIntent : ISchemaKeywordIntent
    {
        private readonly SchemaHelper.RelationInfo[] _relations;

        public RelationsIntent(IEnumerable<SchemaHelper.RelationInfo> relations)
        {
            _relations = relations.ToArray();
        }

        public void Apply(JsonSchemaBuilder builder)
        {
            JsonArray array = new JsonArray();
            foreach (var relation in _relations)
            {
                JsonObject obj = new JsonObject
                {
                    { "from", relation.FromFieldName.Camelize() },
                    {
                        "to", new JsonObject
                        {
                            { "table", relation.ToTableName.Camelize() },
                            { "field", relation.ToFieldName.Camelize() }
                        }
                    }
                };
                array.Add(obj);
            }

            builder.Unrecognized("relations", array);
        }
    }
}