using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;
using Magus.Json.Keywords;

namespace Magus.Json
{
    internal class PrimaryKeyIntent : ISchemaKeywordIntent
    {
        private readonly string _fieldName;

        public PrimaryKeyIntent(string fieldName)
        {
            _fieldName = fieldName;
        }

        public void Apply(JsonSchemaBuilder builder)
        {
            JsonObject obj = new JsonObject { { PrimaryKeyKeyword.FieldName, _fieldName } };
            builder.Unrecognized(PrimaryKeyKeyword.Name, obj);
        }
    }
}