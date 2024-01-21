using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Magus.Generator;

public partial class TypeMeta
{
    private readonly ReferenceSymbols _referenceSymbols;
    private DiagnosticDescriptor? _constructorDiagnosticDescriptor;
    public INamedTypeSymbol Symbol { get; }
    public string TypeName { get; }
    public IMethodSymbol? Constructor { get; }
    public MemberMeta[] Members { get; }
    public MemberMeta PrimaryKey { get; }
    public MemberMeta[] Indexes { get; }
    public MemberMeta[][] CombinedIndexes { get; }
    public bool IsValueType { get; }
    public bool IsUnmanagedType { get; }
    public bool IsInterfaceOrAbstract { get; }

    public TypeMeta(INamedTypeSymbol symbol, ReferenceSymbols referenceSymbols)
    {
        _referenceSymbols = referenceSymbols;
        Symbol = symbol;
        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Constructor = DetermineConstructor(symbol, _referenceSymbols, out _constructorDiagnosticDescriptor);
        Members = symbol
            .GetAllMembers()
            .Where(v => v is (IFieldSymbol or IPropertySymbol) and
                { IsStatic: false, IsImplicitlyDeclared: false, CanBeReferencedByName: true })
            .Reverse()
            .DistinctBy(v => v.Name)
            .Where(v =>
            {
                var include = v.ContainsAttribute(referenceSymbols.MemoryPackIncludeAttribute);
                var ignore = v.ContainsAttribute(referenceSymbols.MemoryPackIgnoreAttribute);
                if (ignore) return false;
                if (include) return true;
                return v is { DeclaredAccessibility: Accessibility.Public };
            })
            .Where(v =>
            {
                if (v is not IPropertySymbol p) return true;
                if (p.IsIndexer) return false;
                if (p.GetMethod == null) return false;
                return true;
            })
            .Select(v => new MemberMeta(v, Constructor, _referenceSymbols))
            .ToArray();

        IsValueType = symbol.IsValueType;
        IsUnmanagedType = symbol.IsUnmanagedType;
        IsInterfaceOrAbstract = symbol.IsAbstract;

        PrimaryKey = Members.First(v => v.IsPrimaryKey);
        var indexes = Members
            .Where(v => v.IsIndexKey)
            .GroupBy(v => v.IndexOrder).ToArray();
        CombinedIndexes = indexes
            .Where(v => v.Count() > 1)
            .Select(v => v.OrderBy(x => x.CombinedIndexOrder).ToArray())
            .ToArray();
        Indexes = indexes
            .Where(v => v.Count() == 1)
            .Select(v => v.Single())
            .ToArray();
    }

    private IMethodSymbol? DetermineConstructor(INamedTypeSymbol symbol, ReferenceSymbols references,
        out DiagnosticDescriptor? invalidConstructorDiagnosticDescriptor)
    {
        invalidConstructorDiagnosticDescriptor = null;
        var ctors = symbol.InstanceConstructors
            .Where(v => !v.IsImplicitlyDeclared)
            .ToArray();

        if (ctors.Length == 0) return null;
        if (!Symbol.IsUnmanagedType && ctors.Length == 1) return ctors[0];
        var ctorAttrs = ctors
            .Where(v => v.ContainsAttribute(references.MagusConstructorAttribute))
            .ToArray();

        if (Symbol.IsUnmanagedType)
        {
            if (ctorAttrs.Length > 0)
            {
                invalidConstructorDiagnosticDescriptor = DiagnosticDescriptors.UnmanagedTypeConstructorWithAttribute;
            }

            return null;
        }

        if (ctorAttrs.Length == 0)
        {
            invalidConstructorDiagnosticDescriptor = DiagnosticDescriptors.MultipleConstructorWithoutAttribute;
            return null;
        }

        if (ctorAttrs.Length == 1)
        {
            return ctorAttrs[0];
        }

        invalidConstructorDiagnosticDescriptor = DiagnosticDescriptors.MultipleConstructorAttributes;
        return null;
    }

    public bool Validate(TypeDeclarationSyntax syntax, IGeneratorContext context)
    {
        var hasError = false;
        if (!Symbol.ContainsAttribute(_referenceSymbols.MemoryPackableAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MissingMemoryPackableAttribute,
                syntax.GetLocation(), Symbol.Name));
            return false;
        }

        if (Symbol.IsRecord)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RecordNotSupported, syntax.GetLocation(),
                Symbol.Name));
            return false;
        }

        if (Symbol.IsRefLikeType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RefLikeTypeNotSupported,
                syntax.GetLocation(), Symbol.Name));
            return false;
        }

        if (_constructorDiagnosticDescriptor != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(_constructorDiagnosticDescriptor, syntax.GetLocation(),
                Symbol.Name));
            hasError = true;
        }

        if (Members.Count(v => v.IsPrimaryKey) > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultiplePrimaryKeys, syntax.GetLocation(),
                Symbol.Name));
            hasError = true;
        }

        if (CombinedIndexes.Any(v => v.DistinctBy(x => x.CombinedIndexOrder).Count() != v.Length))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.CombinedIndexOrdersNotUnique,
                syntax.GetLocation(), Symbol.Name));
            hasError = true;
        }

        return !hasError;
    }
}

public partial class MemberMeta
{
    public static readonly MemberMeta Empty = new();
    public ISymbol Symbol { get; }
    public string Name { get; }
    public ITypeSymbol MemberType { get; }
    public bool IsField { get; }
    public bool IsProperty { get; }
    public bool IsReadOnly { get; }
    public bool IsAssignable { get; }
    public bool IsConstructorParameter { get; }
    public string? ConstructorParameterName { get; }

    public bool IsPrimaryKey { get; }
    public bool IsIndexKey { get; }
    public int IndexOrder { get; }
    public int CombinedIndexOrder { get; }

    public string ContainingNamespace { get; }

    private MemberMeta()
    {
        Symbol = null!;
        Name = null!;
        MemberType = null!;
        ContainingNamespace = null!;
    }

    public MemberMeta(ISymbol symbol, IMethodSymbol? constructor, ReferenceSymbols references)
    {
        Symbol = symbol;
        Name = symbol.Name;

        if (constructor != null)
        {
            IsConstructorParameter = constructor.TryGetConstructorParameter(symbol, out var constructorParameterName);
            ConstructorParameterName = constructorParameterName;
        }
        else
        {
            IsConstructorParameter = false;
        }

        if (symbol is IFieldSymbol f)
        {
            IsProperty = false;
            IsField = true;
            MemberType = f.Type;
            IsReadOnly = f.IsReadOnly;
#if ROSLYN_UNITY
            IsAssignable = f is { IsReadOnly: false };
#else
            IsAssignable = f is { IsReadOnly: false, IsRequired: false };
#endif
        }
        else if (symbol is IPropertySymbol p)
        {
            IsProperty = true;
            IsField = false;
            MemberType = p.Type;
            IsReadOnly = p.IsReadOnly;
#if ROSLYN_UNITY
            IsAssignable = p is { IsReadOnly: false, SetMethod.IsInitOnly: false };
#else
            IsAssignable = p is { IsReadOnly: false, IsRequired: false, SetMethod.IsInitOnly: false };
#endif
        }
        else
        {
            throw new Exception("Unknown symbol type");
        }

        ContainingNamespace = MemberType.ContainingNamespace.ToDisplayString();

        var primaryKeyAttribute = symbol.GetAttribute(references.PrimaryKeyAttribute);
        IsPrimaryKey = primaryKeyAttribute != null;

        var indexAttribute = symbol.GetAttribute(references.IndexAttribute);
        if (indexAttribute != null)
        {
            IsIndexKey = true;
            IndexOrder = indexAttribute.ConstructorArguments[0].Value! as int? ?? 0;
            CombinedIndexOrder = indexAttribute.ConstructorArguments[1].Value! as int? ?? 0;
        }
    }
}