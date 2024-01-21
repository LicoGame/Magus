using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Magus.Generator;

public interface IGeneratorContext
{
    LanguageVersion LanguageVersion { get; }
    bool Net7OrGreater { get; }
    CancellationToken CancellationToken { get; }
    void AddSource(string name, string source);
    void ReportDiagnostic(Diagnostic diagnostic);
}

public static class GeneratorContextExtensions
{
    public static bool IsCSharp9OrGreater(this IGeneratorContext context)
    {
        return (int)context.LanguageVersion >= 900; // C# 9 == 900
    }

    public static bool IsCSharp10OrGreater(this IGeneratorContext context)
    {
        return (int)context.LanguageVersion >= 1000; // C# 10 == 1000
    }

    public static bool IsCSharp11OrGreater(this IGeneratorContext context)
    {
        return (int)context.LanguageVersion >= 1100; // C# 11 == 1100
    }
}
