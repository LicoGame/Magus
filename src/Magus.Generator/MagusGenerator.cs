using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Magus.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MagusGenerator : IIncrementalGenerator
{
    public const string MagusTableAttributeFullName = "Magus.MagusTableAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tableProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MagusTableAttributeFullName,
                predicate: static (node, token) =>
                {
                    return node is ClassDeclarationSyntax;
                },
                transform: static (context, token) =>
                {
                    return (TypeDeclarationSyntax)context.TargetNode;
                });
        
        var parseProvider = context.ParseOptionsProvider.Select((parseOptions, token) =>
        {
            var csOptions = (CSharpParseOptions)parseOptions;
            var langVersion = csOptions.LanguageVersion;
            var net7 = csOptions.PreprocessorSymbolNames.Contains("NET7_0_OR_GREATER");
            return (langVersion, net7);
        });
        
        var source = tableProvider
            .Combine(context.CompilationProvider)
            .WithComparer(Comparer.Instance)
            .Combine(parseProvider);
        
        context.RegisterSourceOutput(source, static (context, source) =>
        {
            var (typeDeclaration, compilation) = source.Left;
            var (langVersion, net7) = source.Right;
            Generate(typeDeclaration, compilation, new GeneratorContext(context, langVersion, net7));
        });

        var collected = tableProvider
            .Collect()
            .Combine(context.CompilationProvider)
            .WithComparer(MultiComparer.Instance)
            .Combine(parseProvider);
        context.RegisterSourceOutput(collected, static (context, source) =>
        {
            var (typeDeclarations, compilation) = source.Left;
            var (langVersion, net7) = source.Right;
            Generate(typeDeclarations, compilation, new GeneratorContext(context, langVersion, net7));
        });
    }

    class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new Comparer();
        
        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
        {
            return x.Item1.Equals(y.Item1);
        }

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
        {
            unchecked
            {
                return obj.Item1.GetHashCode();
            }
        }
    }

    class MultiComparer : IEqualityComparer<(ImmutableArray<TypeDeclarationSyntax>, Compilation)>
    {
        public static readonly MultiComparer Instance = new MultiComparer();

        public bool Equals((ImmutableArray<TypeDeclarationSyntax>, Compilation) x,
            (ImmutableArray<TypeDeclarationSyntax>, Compilation) y)
        {
            return x.Item1.SequenceEqual(y.Item1);
        }

        public int GetHashCode((ImmutableArray<TypeDeclarationSyntax>, Compilation) obj)
        {
            unchecked
            {
                return obj.Item1.GetHashCode();
            }
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        readonly SourceProductionContext _context;
        public LanguageVersion LanguageVersion { get; }
        public bool Net7OrGreater { get; }
        public CancellationToken CancellationToken => _context.CancellationToken;

        public GeneratorContext(SourceProductionContext context, LanguageVersion langVersion, bool net7OrGreater)
        {
            LanguageVersion = langVersion;
            Net7OrGreater = net7OrGreater;
            _context = context;
        }
        
        public void AddSource(string name, string source)
        {
            _context.AddSource(name, source);
        }
        
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }
    }
}