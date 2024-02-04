using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public class DiagnosticDescriptors
{
    private const string Category = "Magus";
    
    public static readonly DiagnosticDescriptor MustBePartial = 
        new DiagnosticDescriptor(
            id: "MAGUS001",
            title: "Magus table object must be partial",
            messageFormat: "Magus table object '{0}' must be partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor NestedNotAllow =
        new DiagnosticDescriptor(
            id: "MAGUS002",
            title: "Magus table object must not be nested",
            messageFormat: "Magus table object '{0}' must not be nested",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor UnmanagedTypeConstructorWithAttribute =
        new DiagnosticDescriptor(
            id: "MAGUS003",
            title: "Unmanaged type constructor must not have [MagusConstructor] attribute",
            messageFormat: "Unmanaged type constructor '{0}' must not have [MagusConstructor] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleConstructorWithoutAttribute =
        new DiagnosticDescriptor(
            id: "MAGUS004",
            title: "Require [MagusConstructor] attribute at least one",
            messageFormat: "Magus table object require [MagusConstructor] attribute at least one, when object has the multiple constructors",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MultipleConstructorAttributes =
        new DiagnosticDescriptor(
            id: "MAGUS005",
            title: "Require only one [MagusConstructor] attribute",
            messageFormat: "Magus table object require only one [MagusConstructor] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MultiplePrimaryKeys =
        new DiagnosticDescriptor(
            id: "MAGUS006",
            title: "Require only one [PrimaryKey] attribute",
            messageFormat: "Magus table object require only one [PrimaryKey] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MissingMemoryPackableAttribute =
        new DiagnosticDescriptor(
            id: "MAGUS007",
            title: "Magus table object must have [MemoryPackable] attribute",
            messageFormat: "Magus table object '{0}' must have [MemoryPackable] attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RefLikeTypeNotSupported =
        new DiagnosticDescriptor(
            id: "MAGUS008",
            title: "Magus table object must not be ref-like type",
            messageFormat: "Magus table object '{0}' must not be ref-like type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor RecordNotSupported =
        new DiagnosticDescriptor(
            id: "MAGUS009",
            title: "Magus table object must not be record type",
            messageFormat: "Magus table object '{0}' must not be record type",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor CombinedIndexOrdersNotUnique =
        new DiagnosticDescriptor(
            id: "MAGUS010",
            title: "Combined index orders must be unique",
            messageFormat: "Magus table object '{0}' combined index orders must be unique",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static DiagnosticDescriptor UnexpectedError(Exception e) =>
        new DiagnosticDescriptor(
            id: "MAGUS999",
            title: "Unexpected error",
            messageFormat: $"{e}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
}