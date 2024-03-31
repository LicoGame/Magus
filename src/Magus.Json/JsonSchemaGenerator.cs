using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace Magus.Json
{
    public class JsonSchemaGenerator
    {
        public static void GenerateAll<T>(
            Func<Type, MemoryStream> streamCollector,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
            where T : MagusDatabaseBase, new()
        {
            var instance = new T();
            var tableTypes = instance.TableTypes;
            var configuration = GetConfiguration();
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            foreach (var t in tableTypes)
            {
                var stream = streamCollector(t);
                var arrayT = t.MakeArrayType();
                GenerateInternal(arrayT, stream, configuration, writerOptions, serializerOptions, generators, refiners);
            }
        }

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

            var configuration = GetConfiguration();
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            GenerateInternal(t, stream, configuration, writerOptions, serializerOptions, generators, refiners);
        }

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
            var configuration = GetConfiguration();
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            GenerateInternal(arrayT, stream, configuration, writerOptions, serializerOptions, generators, refiners);
        }

        private static SchemaGeneratorConfiguration GetConfiguration()
        {
            var configuration = new SchemaGeneratorConfiguration();
            configuration.Refiners.Add(new PrimaryKeyRefiner());
            return configuration;
        }

        private static void GenerateInternal(
            Type type,
            Stream stream,
            SchemaGeneratorConfiguration configuration,
            JsonWriterOptions writerOptions,
            JsonSerializerOptions serializerOptions,
            IReadOnlyCollection<ISchemaGenerator>? generators = null,
            IReadOnlyCollection<ISchemaRefiner>? refiners = null)
        {
            if (generators != null)
            {
                configuration.Generators.AddRange(generators);
            }
            if (refiners != null)
            {
                configuration.Refiners.AddRange(refiners);
            }
            var builder = new JsonSchemaBuilder();
            var schema = builder
                .FromType(type, configuration)
                .Build();
            var converter = new SchemaJsonConverter();
            var writer = new Utf8JsonWriter(stream, writerOptions);
            converter.Write(writer, schema, serializerOptions);
            writer.Flush();
        }
    }

    internal class PrimaryKeyRefiner : ISchemaRefiner
    {
        public bool ShouldRun(SchemaGenerationContextBase context)
        {
            return context.GetAttributes().FirstOrDefault(attr => attr is PrimaryKeyAttribute) != null;
        }

        public void Run(SchemaGenerationContextBase context)
        {
            context.Intents.Add(new UniqueItemsIntent(true));
        }
    }
}