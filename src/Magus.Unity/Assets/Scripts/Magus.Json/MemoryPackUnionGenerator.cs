using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;
using MemoryPack;

namespace Magus.Json
{
    public sealed class MemoryPackUnionGenerator : ISchemaGenerator
    {
        public const string TagKeyword = "$tag";

        public bool Handles(Type type)
        {
            if (!type.IsInterface) return false;
            var attributes = MemoryPackUnionAttributes(type);

            return attributes.Count > 0;
        }

        private static List<MemoryPackUnionAttribute> MemoryPackUnionAttributes(Type type)
        {
            var attributes =
                type.GetCustomAttributes<MemoryPackUnionAttribute>()
                    .OrderBy(attr => attr.Tag)
                    .ToList();
            return attributes;
        }

        public void AddConstraints(SchemaGenerationContextBase context)
        {
            var oneOf = new OneOfIntent();
            var attributes = MemoryPackUnionAttributes(context.Type);
            foreach (var attribute in attributes)
            {
                var ctx = SchemaGenerationContextCache.Get(attribute.Type);
                // Inject tag into properties
                var propertiesIntent = (PropertiesIntent?)ctx.Intents.FirstOrDefault(v => v is PropertiesIntent);
                propertiesIntent?.Properties.Add(TagKeyword, ConstantGenerationContext.Int(attribute.Tag));
                oneOf.Subschemas.Add(ctx.Intents);
            }

            context.Intents.Add(new TypeIntent(SchemaValueType.Object));
            context.Intents.Add(oneOf);
        }
    }

    public sealed class MemoryPackUnionConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsInterface && typeToConvert.GetCustomAttributes<MemoryPackUnionAttribute>().Any();
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var attributes = typeToConvert.GetCustomAttributes<MemoryPackUnionAttribute>();
            var converter =
                (JsonConverter)Activator.CreateInstance(
                    typeof(MemoryPackUnionConverter<>).MakeGenericType(typeToConvert),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: new object[] { attributes },
                    culture: null)!;

            return converter;
        }
    }

    public sealed class MemoryPackUnionConverter<T> : WeaklyTypedJsonConverter<T>
    {
        private readonly Dictionary<ushort, MemoryPackUnionAttribute> _attributes;

        public MemoryPackUnionConverter(IEnumerable<MemoryPackUnionAttribute> attributes)
        {
            _attributes = attributes.ToDictionary(v => v.Tag, v => v);
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected object");

            var dictionary = options.ReadDictionary(ref reader,
                JsonSchemaRelationExtSerializerInternalContext.Default.JsonElement);

            if (dictionary == null || dictionary!.Count == 0)
                return default;
            var tag = dictionary[MemoryPackUnionGenerator.TagKeyword];
            if (!_attributes.TryGetValue((ushort)tag.GetInt32(), out var attribute))
                throw new JsonException("Unknown tag");

            dictionary.Remove(MemoryPackUnionGenerator.TagKeyword);

            var instance = Activator.CreateInstance(attribute.Type);

            if (instance == null)
                throw new JsonException($"Could not create instance of {attribute.Type}");

            bool TryGetValueByCaseInsensitive(string key, out JsonElement value)
            {
                var kv = dictionary.FirstOrDefault(kv => kv.Key.ToLowerInvariant() == key.ToLowerInvariant());
                if (kv.Equals(default))
                {
                    value = default;
                    return false;
                }

                value = kv.Value;
                dictionary.Remove(kv.Key);
                return true;
            }

            // Write properties with reflection
            foreach (var property in attribute.Type.GetProperties())
            {
                if (dictionary.Remove(property.Name, out var value) ||
                    (options.PropertyNamingPolicy != null &&
                     dictionary.Remove(options.PropertyNamingPolicy.ConvertName(property.Name), out value)) ||
                    (options.PropertyNameCaseInsensitive == true &&
                     TryGetValueByCaseInsensitive(property.Name, out value)))
                {
                    var actualValue = value.Deserialize(property.PropertyType, options);
                    property.SetValue(instance, actualValue);
                }
            }

            if (dictionary.Count > 0)
                throw new JsonException($"Unknown properties {string.Join(", ", dictionary.Keys)}");

            return (T)instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            // Detect tag from value can assign into attributes type
            var tag = _attributes.Values.FirstOrDefault(attr => attr.Type == value.GetType());
            if (tag == null)
                throw new JsonException($"Could not find tag for type {value.GetType()}");
            writer.WriteStartObject();
            // Write tag
            writer.WritePropertyName(MemoryPackUnionGenerator.TagKeyword);
            writer.WriteNumberValue(tag.Tag);
            // Write properties with reflection
            foreach (var property in value.GetType().GetProperties())
            {
                writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name);
                options.Write(writer, property.GetValue(value), property.PropertyType);
            }

            writer.WriteEndObject();
        }
    }
}