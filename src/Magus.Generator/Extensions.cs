using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Magus.Generator;

internal static class Extensions
{
    private const string UnderScorePrefix = "_";
    
    public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribute));
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        return symbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attribute));
    }
    
    public static bool TryGetConstructorParameter(this IMethodSymbol constructor, ISymbol member, out string? constructorParameterName)
    {
        var constructorParameter = GetConstructorParameter(constructor, member.Name);
        if (constructorParameter == null && member.Name.StartsWith(UnderScorePrefix))
        {
            constructorParameter = GetConstructorParameter(constructor, member.Name.Substring(UnderScorePrefix.Length));
        }

        constructorParameterName = constructorParameter?.Name;
        return constructorParameter != null;

        static IParameterSymbol? GetConstructorParameter(IMethodSymbol constructor, string name) => constructor.Parameters.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsConstructorParameter(this IEnumerable<MemberMeta> members, IParameterSymbol constructorParameter) =>
        members.Any(x =>
            x.IsConstructorParameter &&
            string.Equals(constructorParameter.Name, x.ConstructorParameterName, StringComparison.OrdinalIgnoreCase));
    
    public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol, bool withoutOverride = true)
    {
        // Iterate Parent -> Derived
        if (symbol.BaseType != null)
        {
            foreach (var item in GetAllMembers(symbol.BaseType))
            {
                // override item already iterated in parent type
                if (!withoutOverride || !item.IsOverride)
                {
                    yield return item;
                }
            }
        }

        foreach (var item in symbol.GetMembers())
        {
            if (!withoutOverride || !item.IsOverride)
            {
                yield return item;
            }
        }
    }
    
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => DistinctBy(source, keySelector, null);

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        return DistinctByIterator(source, keySelector, comparer);
    }

    private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        using IEnumerator<TSource> enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var set = new HashSet<TKey>(comparer);
            do
            {
                TSource element = enumerator.Current;
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
            while (enumerator.MoveNext());
        }
    }
    
    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrEmpty(s) || char.IsLower(s, 0))
        {
            return s;
        }

        var array = s.ToCharArray();
        array[0] = char.ToLowerInvariant(array[0]);
        return new string(array);
    }
    
    public static string ToPascalCase(this string s)
    {
        if (string.IsNullOrEmpty(s) || char.IsUpper(s, 0))
        {
            return s;
        }
        
        var array = s.ToCharArray();
        array[0] = char.ToUpperInvariant(array[0]);
        return new string(array);
    }
}