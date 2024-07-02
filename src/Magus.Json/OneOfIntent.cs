using System;
using System.Collections.Generic;
using System.Linq;
using Json.Schema;
using Json.Schema.Generation;

namespace Magus.Json
{
    public class OneOfIntent : ISchemaKeywordIntent
    {
        public OneOfIntent(List<IEnumerable<ISchemaKeywordIntent>> subschemas)
        {
            Subschemas = subschemas;
        }
        
        public OneOfIntent(params IEnumerable<ISchemaKeywordIntent>[] subschemas)
        {
            Subschemas = new List<IEnumerable<ISchemaKeywordIntent>>(subschemas);
        }

        /// <summary>
        /// Gets the subschemas to include.
        /// </summary>
        public List<IEnumerable<ISchemaKeywordIntent>> Subschemas { get; }

        public void Apply(JsonSchemaBuilder builder)
        {
            builder.Add(new OneOfKeyword(Subschemas.Select(Build)));
        }
        
        private static JsonSchema Build(IEnumerable<ISchemaKeywordIntent> subschema)
        {
            var builder = new JsonSchemaBuilder();

            foreach (var intent in subschema)
            {
                intent.Apply(builder);
            }

            return builder;
        }
    }
}