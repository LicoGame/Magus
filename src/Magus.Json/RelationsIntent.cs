using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Humanizer;
using Json.Schema;
using Json.Schema.Generation;
using Magus.Json.Keywords;

namespace Magus.Json
{
    internal class RelationsIntent : ISchemaKeywordIntent
    {
        private readonly RelationsKeyword.RelationInfo[] _relations;

        public RelationsIntent(IEnumerable<RelationsKeyword.RelationInfo> relations)
        {
            _relations = relations.ToArray();
        }

        public void Apply(JsonSchemaBuilder builder)
        {
            builder.Relations(_relations);
            // TODO: RelationsKeywordに置き換え
            // JsonArray array = new JsonArray();
            // foreach (var relation in _relations)
            // {
            //     JsonObject obj = new JsonObject
            //     {
            //         { RelationsKeyword.FromName, relation.FromFieldName.Camelize() },
            //         {
            //             RelationsKeyword.ToName, new JsonObject
            //             {
            //                 { RelationsKeyword.TableName, relation.ToTableName.Camelize() },
            //                 { RelationsKeyword.FieldName, relation.ToFieldName.Camelize() }
            //             }
            //         }
            //     };
            //     array.Add(obj);
            // }
            //
            // builder.Unrecognized(RelationsKeyword.Name, array);
        }
    }
}