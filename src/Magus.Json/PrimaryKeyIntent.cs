using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;

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
            JsonObject obj = new JsonObject { { "field", _fieldName } };
            builder.Unrecognized("primaryKey", obj);
        }
    }
}