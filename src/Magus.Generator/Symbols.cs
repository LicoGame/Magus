using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public class ReferenceSymbols
{
    public Compilation Compilation { get; }
    public INamedTypeSymbol MagusTableAttribute { get; }
    public INamedTypeSymbol PrimaryKeyAttribute { get; }
    public INamedTypeSymbol IndexAttribute { get; }
    public INamedTypeSymbol MagusConstructorAttribute { get; }
    
    #region MemoryPack
    
    public INamedTypeSymbol MemoryPackableAttribute { get; }
    public INamedTypeSymbol MemoryPackIncludeAttribute { get; }
    public INamedTypeSymbol MemoryPackIgnoreAttribute { get; }
    
    #endregion
    
    public ReferenceSymbols(Compilation compilation)
    {
        Compilation = compilation;
        
        MagusTableAttribute = Compilation.GetTypeByMetadataName(Generator.MagusTableAttributeFullName)!;
        PrimaryKeyAttribute = Compilation.GetTypeByMetadataName("Magus.PrimaryKeyAttribute")!;
        IndexAttribute = Compilation.GetTypeByMetadataName("Magus.IndexAttribute")!;
        MagusConstructorAttribute = Compilation.GetTypeByMetadataName("Magus.MagusConstructorAttribute")!;
        MemoryPackableAttribute = Compilation.GetTypeByMetadataName("MemoryPack.MemoryPackableAttribute")!;
        MemoryPackIncludeAttribute = Compilation.GetTypeByMetadataName("MemoryPack.MemoryPackIncludeAttribute")!;
        MemoryPackIgnoreAttribute = Compilation.GetTypeByMetadataName("MemoryPack.MemoryPackIgnoreAttribute")!;
    }
}