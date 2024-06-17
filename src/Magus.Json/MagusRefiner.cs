using System.Collections.Generic;
using System.Linq;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace Magus.Json
{
    internal class MagusRefiner : ISchemaRefiner
    {
        public bool ShouldRun(SchemaGenerationContextBase context)
        {
            var typeIntent = context.Intents.OfType<TypeIntent>().FirstOrDefault();
            var itemsIntent = context.Intents.OfType<ItemsIntent>().FirstOrDefault();
            return itemsIntent != null &&
                   typeIntent != null &&
                   itemsIntent.Context.GetAttributes().Any(attr => attr is MagusTableAttribute);
        }

        public void Run(SchemaGenerationContextBase context)
        {
            var itemsIntent = context.Intents.OfType<ItemsIntent>().FirstOrDefault();
            var typeContext = itemsIntent?.Context;
            if (typeContext == null)
            {
                return;
            }

            var propertiesIntent = typeContext.Intents.OfType<PropertiesIntent>().FirstOrDefault();
            (string Name, SchemaGenerationContextBase Context)? primaryKeyContext = null;
            List<(string Name, SchemaGenerationContextBase Context)> relationContexts = new();
            foreach (var property in propertiesIntent!.Properties)
            {
                var propCtx = property.Value;
                var attributes = propCtx.GetAttributes().ToArray();
                if (!attributes.OfType<PrimaryKeyAttribute>().Any() && !attributes.OfType<RelationAttribute>().Any())
                {
                    continue;
                }

                if (attributes.OfType<PrimaryKeyAttribute>().Any())
                {
                    primaryKeyContext = (property.Key, propCtx);
                }

                if (attributes.OfType<RelationAttribute>().Any())
                {
                    relationContexts.Add((property.Key, propCtx));
                }
            }

            if (primaryKeyContext != null)
            {
                context.Intents.Add(new PrimaryKeyIntent(primaryKeyContext.Value.Name));
            }

            if (relationContexts.Any())
            {
                context.Intents.Add(new RelationsIntent(relationContexts.Select(x =>
                {
                    var relation = x.Context.GetAttributes().OfType<RelationAttribute>().FirstOrDefault();
                    return new SchemaHelper.RelationInfo(x.Name, relation!.TableName, relation.FieldName);
                })));
            }
        }
    }
}