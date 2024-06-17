using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;

namespace Magus.Json
{
    public class JsonSchemaGenerator
    {
#if NET8_0_OR_GREATER
        [RequiresDynamicCode("This method uses reflection to query types and is not suited for AOT scenarios.")]
#endif
        public static void GenerateAll<T>(
            Func<Type, MemoryStream> streamCollector,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
            where T : MagusDatabaseBase, new()
        {
            var instance = new T();
            var tableTypes = instance.TableTypes;
            foreach (var t in tableTypes)
            {
                var stream = streamCollector(t);
                var arrayT = t.MakeArrayType();
                GenerateInternal(arrayT, stream, generators: generators, refiners: refiners);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode("This method uses reflection to query types and is not suited for AOT scenarios.")]
#endif
        public static Dictionary<Type, MemoryStream> GenerateAll<T>(
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
            where T : MagusDatabaseBase, new()
        {
            var streamCollector = new Dictionary<Type, MemoryStream>();
            var tableTypes = new T().TableTypes;
            foreach (var t in tableTypes)
            {
                streamCollector[t] = new MemoryStream();
            }

            GenerateAll<T>(t => streamCollector[t], generators, refiners);
            return streamCollector;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode("This method uses reflection to query types and is not suited for AOT scenarios.")]
#endif
        public static void GenerateObject<T>(Stream stream,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
        {
            var t = typeof(T);
            var tableAttribute = t.GetCustomAttribute<MagusTableAttribute>();
            if (tableAttribute == null)
            {
                throw new InvalidOperationException("Table type must be marked with [MagusTableAttribute]");
            }

            GenerateInternal(t, stream, generators: generators, refiners: refiners);
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode("This method uses reflection to query types and is not suited for AOT scenarios.")]
#endif
        public static void GenerateArray<T>(Stream stream,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
        {
            var t = typeof(T);
            var tableAttribute = t.GetCustomAttribute<MagusTableAttribute>();
            if (tableAttribute == null)
            {
                throw new InvalidOperationException("Table type must be marked with [MagusTableAttribute]");
            }

            var arrayT = typeof(T[]);
            GenerateInternal(arrayT, stream, generators: generators, refiners: refiners);
        }
        
        private static SchemaGeneratorConfiguration GetConfiguration()
        {
            var configuration = new SchemaGeneratorConfiguration();
            configuration.Refiners.Add(new MagusRefiner());
            configuration.PropertyNameResolver = PropertyNameResolvers.CamelCase;
            configuration.Optimize = false;
            return configuration;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode("This method uses reflection to query types and is not suited for AOT scenarios.")]
#endif
        private static void GenerateInternal(
            Type type,
            Stream stream,
            SchemaGeneratorConfiguration? configuration = null,
            JsonWriterOptions? writerOptions = null,
            JsonSerializerOptions? serializerOptions = null,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
        {
            configuration ??= GetConfiguration();
            if (generators != null)
            {
                configuration.Generators.AddRange(generators);
            }

            if (refiners != null)
            {
                configuration.Refiners.AddRange(refiners);
            }

            writerOptions ??= new JsonWriterOptions
            {
                Indented = true
            };
            serializerOptions ??= new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            // NOTE: Mark as using type signature for IL2CPP on Unity
            // But these don't work for AOT scenarios
            AttributeHandler.RemoveHandler<PrimaryKeyAttributeHandler>();
            AttributeHandler.AddHandler<PrimaryKeyAttributeHandler>();
            AttributeHandler.RemoveHandler<RelationAttributeHandler>();
            AttributeHandler.AddHandler<RelationAttributeHandler>();

            var builder = new JsonSchemaBuilder();
            var schema = builder
                .Schema(MetaSchemas.Draft7Id)
                .FromType(type, configuration)
                .Build();
            var converter = new SchemaJsonConverter();
            using var writer = new Utf8JsonWriter(stream, writerOptions.Value);
            converter.Write(writer, schema, serializerOptions);
            writer.Flush();
        }
    }
}