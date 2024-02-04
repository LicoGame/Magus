using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public class TypeCollector
{
    private readonly List<INamedTypeSymbol> _types = new();
    public IReadOnlyList<INamedTypeSymbol> Types => _types;

    public static TypeCollector Create(in IEnumerable<TypeMeta> metas)
    {
        var collector = new TypeCollector();
        var typeSymbols = metas.Select(v => v.Symbol);
        var memberSymbols = metas
            .SelectMany(v => v.Members.Select(m => m.MemberType));
        collector._types.AddRange(typeSymbols
            .Concat(memberSymbols)
            .Distinct(SymbolEqualityComparer.Default)
            .Where(v => v is INamedTypeSymbol)
            .Cast<INamedTypeSymbol>());

        return collector;
    }
}