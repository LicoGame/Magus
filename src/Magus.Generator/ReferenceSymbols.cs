using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Magus.Generator;

public class ReferenceSymbols
{
    public Compilation Compilation { get; }
    public INamedTypeSymbol MagusTableAttribute { get; }
    public INamedTypeSymbol PrimaryKeyAttribute { get; }
    public INamedTypeSymbol IndexAttribute { get; }
    public INamedTypeSymbol MagusConstructorAttribute { get; }
    public INamedTypeSymbol MemoryPackableInterface { get; }

    private string _locationHint = string.Empty;
    
    #region MemoryPack
    
    public INamedTypeSymbol MemoryPackableAttribute { get; }
    public INamedTypeSymbol MemoryPackIncludeAttribute { get; }
    public INamedTypeSymbol MemoryPackIgnoreAttribute { get; }
    
    #endregion

    public ReferenceSymbols(Compilation compilation, in ImmutableArray<TypeDeclarationSyntax> syntaxes)
    : this(compilation)
    {
        _locationHint = string.Join(",", syntaxes.Select(s => s.GetLocation().ToString()));
    }

    public ReferenceSymbols(Compilation compilation, TypeDeclarationSyntax syntax)
        : this(compilation)
    {
        _locationHint = syntax.GetLocation().ToString();
    }
    
    private ReferenceSymbols(Compilation compilation)
    {
        Compilation = compilation;
        
        MagusTableAttribute = GetTypeByMetadataName(MagusGenerator.MagusTableAttributeFullName)!;
        PrimaryKeyAttribute = GetTypeByMetadataName("Magus.PrimaryKeyAttribute")!;
        IndexAttribute = GetTypeByMetadataName("Magus.IndexAttribute")!;
        MagusConstructorAttribute = GetTypeByMetadataName("Magus.MagusConstructorAttribute")!;
        MemoryPackableAttribute = GetTypeByMetadataName("MemoryPack.MemoryPackableAttribute")!;
        MemoryPackIncludeAttribute = GetTypeByMetadataName("MemoryPack.MemoryPackIncludeAttribute")!;
        MemoryPackIgnoreAttribute = GetTypeByMetadataName("MemoryPack.MemoryPackIgnoreAttribute")!;
        MemoryPackableInterface = GetTypeByMetadataName("MemoryPack.IMemoryPackable`1")!.ConstructUnboundGenericType();
    }
    
    INamedTypeSymbol GetTypeByMetadataName(string metadataName)
    {
        var symbol = Compilation.GetTypeByMetadataName(metadataName);
        if (symbol == null)
        {
            throw new InvalidOperationException($"Type {metadataName} is not found in compilation at:{_locationHint}");
        }
        return symbol;
    }
}