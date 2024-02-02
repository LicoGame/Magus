using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public static class Helper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ResolveType(this ITypeSymbol symbol)
    {
        static string ResolveArrayType(ITypeSymbol symbol)
        {
            var array = (IArrayTypeSymbol)symbol;
            return $"{ResolveType(array.ElementType)}[]";
        }

        return symbol.SpecialType switch
        {
            SpecialType.System_Object => "object",
            SpecialType.System_String => "string",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Int16 => "short",
            SpecialType.System_Int32 => "int",
            SpecialType.System_Int64 => "long",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_DateTime => "DateTime",
            SpecialType.System_Array => ResolveArrayType(symbol),
            _ => symbol.Name
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldRegisterType(this ITypeSymbol symbol, in ReferenceSymbols referenceSymbols)
    {
        var isManagedStruct = symbol is { IsUnmanagedType: false, SpecialType: SpecialType.None, TypeKind: TypeKind.Struct };
        var implementsMemoryPackable = isManagedStruct && symbol.AllInterfaces.Select(t => t.ConstructUnboundGenericType()).Contains(referenceSymbols.MemoryPackableInterface, SymbolEqualityComparer.Default);
        var annotatedMemoryPackable = isManagedStruct && symbol.GetAttribute(referenceSymbols.MemoryPackableAttribute) != null;
        return implementsMemoryPackable || annotatedMemoryPackable; 
    }
}