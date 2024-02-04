using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public static class Assertion
{
    public static void IsNotNull<T>(T? value, IGeneratorContext context) where T : class
    {
        if (value == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NullReferenceError<T>(), Location.None));
        }
    }
}