﻿using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Magus.Generator;

public partial class MagusGenerator
{
    private static void Generate(TypeDeclarationSyntax syntax, Compilation compilation, GeneratorContext context)
    {
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var typeSymbol = semanticModel.GetDeclaredSymbol(syntax);
        if (typeSymbol == null) return;
        if (!IsPartial(syntax))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, syntax.GetLocation(),
                syntax.Identifier));
            return;
        }

        if (IsNested(syntax))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NestedNotAllow, syntax.GetLocation(),
                syntax.Identifier));
            return;
        }

        var referenceSymbols = new ReferenceSymbols(compilation, typeSymbol);
        try
        {
            var typeMeta = new TypeMeta(typeSymbol, referenceSymbols);

            if (!typeMeta.Validate(syntax, context))
            {
                return;
            }

            var fullType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "")
                .Replace("<", "_")
                .Replace(">", "_");

            {
                var sb = new IndentedStringBuilder();

                sb.AppendLine("""
                              // <auto-generated/>
                              using System;
                              using Magus;
                              """);

                sb.AppendLine(typeMeta.Members
                    .Where(v => v.ContainingNamespace != null && v.ContainingNamespace != "System")
                    .Select(v => $"using {v.ContainingNamespace};").Distinct());

                // Namespace
                var ns = typeMeta.Symbol.ContainingNamespace;
                if (!ns.IsGlobalNamespace)
                {
                    using var _ = context.DefineNamespace(sb, ns.ToString());
                    typeMeta.Emit(sb, context);
                }
                else
                {
                    typeMeta.Emit(sb, context);
                }

                context.AddSource($"{fullType}Table.g.cs", sb.ToString());
            }
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnexpectedError(e), syntax.GetLocation()));
        }
    }

    private static void Generate(in ImmutableArray<TypeDeclarationSyntax> syntaxes, Compilation compilation,
        GeneratorContext context)
    {
        var referenceSymbols = new ReferenceSymbols(
            compilation,
            syntaxes.Select(v => compilation.GetSemanticModel(v.SyntaxTree).GetDeclaredSymbol(v)));
        try
        {
            var metas = syntaxes
                .Select(syntax =>
                {
                    var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                    var typeSymbol = semanticModel.GetDeclaredSymbol(syntax);
                    if (typeSymbol == null) return null;
                    var typeMeta = new TypeMeta(typeSymbol, referenceSymbols);
                    return typeMeta;
                })
                .Where(v => v != null)
                .Cast<TypeMeta>()
                .OrderBy(v => v.Symbol.Name)
                .ToArray();

            var importNs = metas
                .SelectMany(m =>
                {
                    var seq = m.Members
                        .Where(v => v.ContainingNamespace != null && v.ContainingNamespace != "System")
                        .Select(v => v.ContainingNamespace);
                    return seq.Append(m.Symbol.ContainingNamespace.ToString());
                })
                .OrderBy(v => v)
                .Distinct()
                .Select(v => $"using {v};")
                .ToArray();

            var typeCollector = TypeCollector.Create(metas);

            if (metas.Length == 0)
                return;

            {
                var sb = new IndentedStringBuilder();

                sb.AppendLine("""
                              // <auto-generated/>
                              using System;
                              using Magus;
                              """);

                sb.AppendLine(importNs);

                {
                    using var _1 = context.DefineNamespace(sb, "Magus.Generated.Builder");
                    sb.AppendLine("public sealed partial class DatabaseBuilder: DatabaseBuilderBase");
                    using var _2 = sb.Block();
                    sb.AppendLine("public DatabaseBuilder()");
                    {
                        using var _ = sb.Block();
                        foreach (var type in typeCollector.Types.Where(v => v.ShouldRegisterType(referenceSymbols)))
                        {
                            type.EmitFormatterRegister(sb, context, referenceSymbols);
                        }
                    }
                    foreach (var typeMeta in metas)
                    {
                        typeMeta.EmitBuilder(sb, context);
                    }
                }

                context.AddSource($"DatabaseBuilder.g.cs", sb.ToString());
            }

            {
                var sb = new IndentedStringBuilder();

                sb.AppendLine("""
                              // <auto-generated/>
                              using System;
                              using Magus;
                              """);

                sb.AppendLine(importNs);

                {
                    using var _1 = context.DefineNamespace(sb, "Magus.Generated.Builder");
                    sb.AppendLine("public sealed partial class LazyDatabaseBuilder: DatabaseBuilderBase");
                    using var _2 = sb.Block();
                    sb.AppendLine("public LazyDatabaseBuilder()");
                    {
                        using var _ = sb.Block();
                        foreach (var type in typeCollector.Types.Where(v => v.ShouldRegisterType(referenceSymbols)))
                        {
                            type.EmitFormatterRegister(sb, context, referenceSymbols);
                        }
                    }
                    foreach (var typeMeta in metas)
                    {
                        typeMeta.EmitLazyBuilder(sb, context);
                    }

                    sb.AppendLine("protected override System.Threading.Tasks.ValueTask OnPreprocessAsync()");
                    {
                        using var _ = sb.Block();
                        foreach (var typeMeta in metas)
                        {
                            typeMeta.EmitLazyBuilderAppendCore(sb, context);
                        }

                        sb.AppendLine("return default;");
                    }
                }

                context.AddSource($"LazyDatabaseBuilder.g.cs", sb.ToString());
            }

            {
                var sb = new IndentedStringBuilder();

                sb.AppendLine("""
                              // <auto-generated/>
                              using System;
                              using Magus;
                              using Magus.Generated.Builder;
                              """);

                sb.AppendLine(importNs);

                {
                    using var _1 = context.DefineNamespace(sb, "Magus.Generated");
                    sb.AppendLine(
                        "using Header = System.Collections.Generic.Dictionary<string, (int offset, int count)>;");
                    sb.AppendLine("public sealed class MagusDatabase : MagusDatabaseBase");
                    using var _2 = sb.Block();

                    // TableTypes
                    {
                        sb.AppendLine($$"""public override Type[] TableTypes =>""");
                        using var _ = sb.Increase();
                        sb.AppendLine(
                            $$"""new Type[] { {{string.Join(", ", metas.Select(v => $"typeof({v.TypeName})"))}} };""");
                    }

                    foreach (var typeMeta in metas)
                    {
                        sb.AppendLine(
                            $$"""public {{typeMeta.TableName}} {{typeMeta.TableName}} { get; private set; }""");
                    }

                    {
                        // Empty Constructor
                        sb.AppendLine("public MagusDatabase()");
                        {
                            using var _ = sb.Block();
                            foreach (var typeMeta in metas)
                            {
                                sb.AppendLine($"this.{typeMeta.TableName} = null!;");
                            }
                        }
                    }

                    {
                        // Construct from each tables
                        sb.AppendLine("public MagusDatabase(");
                        {
                            // Args
                            using var _ = sb.Increase();
                            for (var i = 0; i < metas.Length; i++)
                            {
                                var typeMeta = metas[i];
                                sb.AppendLine(i == metas.Length - 1
                                    ? $"{typeMeta.TableName} {typeMeta.TableName}"
                                    : $"{typeMeta.TableName} {typeMeta.TableName},");
                            }
                        }
                        sb.AppendLine(")");
                        {
                            // Initialize
                            using var _ = sb.Block();
                            foreach (var typeMeta in metas)
                            {
                                sb.AppendLine($"this.{typeMeta.TableName} = {typeMeta.TableName};");
                            }
                        }
                    }
                    {
                        // Construct from binary
                        sb.AppendLine("public MagusDatabase(byte[] binary, int workerSize)");
                        {
                            using var _10 = sb.Increase();
                            sb.AppendLine(": base(binary, workerSize)");
                        }
                        using var _ = sb.Block();
                    }
                    {
                        // Init method
                        sb.AppendLine(
                            "protected override void Init(Header header, System.ReadOnlyMemory<byte> binary, int workerSize)");
                        using var _ = sb.Block();
                        sb.AppendLine("if (workerSize == 1)");
                        {
                            using var _10 = sb.Block();
                            sb.AppendLine("InitSequential(header, binary);");
                        }
                        sb.AppendLine("else");
                        {
                            using var _10 = sb.Block();
                            sb.AppendLine("InitParallel(header, binary, workerSize);");
                        }
                    }

                    static string GenInitializer(TypeMeta typeMeta) =>
                        $"this.{typeMeta.TableName} = ExtractTable<{typeMeta.TypeName.ToPascalCase()},{typeMeta.TableName}>(header, binary, v => new {typeMeta.TableName}(v))";

                    {
                        // Sequential init method
                        sb.AppendLine("private void InitSequential(Header header, System.ReadOnlyMemory<byte> binary)");
                        using var _ = sb.Block();
                        foreach (var typeMeta in metas)
                        {
                            sb.AppendLine($"{GenInitializer(typeMeta)};");
                        }
                    }
                    {
                        // Parallel init method
                        sb.AppendLine(
                            "private void InitParallel(Header header, System.ReadOnlyMemory<byte> binary, int workerSize)");
                        using var _ = sb.Block();
                        sb.AppendLine("var extracts = new Action[]");
                        {
                            // Actions
                            using var _10 = sb.BlockStmt();
                            foreach (var typeMeta in metas)
                            {
                                sb.AppendLine($"() => {GenInitializer(typeMeta)},");
                            }
                        }

                        sb.AppendLine($$"""
                                        System.Threading.Tasks.Parallel.ForEach(extracts, new System.Threading.Tasks.ParallelOptions
                                        {{sb.Pfx}}{ MaxDegreeOfParallelism = workerSize }, action => action.Invoke());
                                        """);
                    }
                    {
                        // ToDatabaseBuilder
                        sb.AppendLine("public DatabaseBuilder ToDatabaseBuilder()");
                        using var _ = sb.Block();
                        sb.AppendLine("var builder = new DatabaseBuilder();");
                        foreach (var typeMeta in metas)
                        {
                            sb.AppendLine($"builder.Append(this.{typeMeta.TableName}.GetRawDataUnsafe());");
                        }

                        sb.AppendLine("return builder;");
                    }
                }

                context.AddSource($"MagusDatabase.g.cs", sb.ToString());
            }
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnexpectedError(e), null));
        }
    }

    private static bool IsPartial(TypeDeclarationSyntax typeDeclaration) =>
        typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    private static bool IsNested(TypeDeclarationSyntax typeDeclaration) =>
        typeDeclaration.Parent is TypeDeclarationSyntax;
}

public static class NamespaceHelper
{
    public static IDisposable DefineNamespace(this IGeneratorContext context, IndentedStringBuilder sb, string ns)
    {
        if (context.IsCSharp10OrGreater())
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
            return Defer.Create();
        }

        sb.AppendLine($"namespace {ns}");
        return sb.Block();
    }
}