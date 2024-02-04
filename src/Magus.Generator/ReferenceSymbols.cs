using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public class ReferenceSymbols
{
    public Compilation Compilation { get; }
    public INamedTypeSymbol MagusTableAttribute { get; }
    public INamedTypeSymbol PrimaryKeyAttribute { get; }
    public INamedTypeSymbol IndexAttribute { get; }
    public INamedTypeSymbol MagusConstructorAttribute { get; }
    public INamedTypeSymbol MemoryPackableInterface { get; }
    
    #region MemoryPack
    
    public INamedTypeSymbol MemoryPackableAttribute { get; }
    public INamedTypeSymbol MemoryPackIncludeAttribute { get; }
    public INamedTypeSymbol MemoryPackIgnoreAttribute { get; }
    
    #endregion
    
    public ReferenceSymbols(Compilation compilation)
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
            throw new InvalidOperationException($"Type {metadataName} is not found in compilation.");
        }
        return symbol;
    }
}