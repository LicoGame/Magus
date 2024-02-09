using System.Text;
using Microsoft.CodeAnalysis;

namespace Magus.Generator;

public partial class TypeMeta
{
    public string TableName => $"{TypeName.ToPascalCase()}Table";

    public void Emit(IndentedStringBuilder sb, IGeneratorContext context)
    {
        var signature = IsValueType switch
        {
            true => "partial struct",
            false => "sealed partial class",
        };

        var nullable = IsValueType ? "" : "?";

        sb.AppendLine($$"""
                        public {{signature}} {{TableName}} : TableBase<{{TypeName}}>
                        """);
        using var _ = sb.Block();
        PrimaryKey.EmitKeySelectorDefinition(sb, TypeName, true);
        foreach (var index in Indexes)
        {
            index.EmitKeySelectorDefinition(sb, TypeName, false);
        }

        foreach (var combinedIndex in CombinedIndexes)
        {
            combinedIndex.EmitCombinedKeySelectorDefinition(sb, TypeName);
        }

        sb.AppendLine();
        EmitConstructor(sb, context);
        sb.AppendLine();

        PrimaryKey.Emit(sb, context, TypeName, true);
        foreach (var index in Indexes)
        {
            index.Emit(sb, context, TypeName, false);
        }

        foreach (var combinedIndex in CombinedIndexes)
        {
            combinedIndex.Emit(sb, TypeName);
        }
    }

    private void EmitConstructor(IndentedStringBuilder sb, IGeneratorContext context)
    {
        sb.AppendLine($"public {TypeName.ToPascalCase()}Table({TypeName}[] sortedData)");
        sb.Increase();
        sb.AppendLine($": base(sortedData)");
        sb.Decrease();
        {
            using var _ = sb.Block();
            PrimaryKey.EmitKeySelectorInitializer(sb, true);
            foreach (var index in Indexes)
            {
                index.EmitKeySelectorInitializer(sb, false);
            }

            foreach (var combinedIndex in CombinedIndexes)
            {
                combinedIndex.EmitCombinedKeySelectorInitializer(sb);
            }
        }
    }

    public void EmitBuilder(IndentedStringBuilder sb, IGeneratorContext context)
    {
        PrimaryKey.EmitBuilder(sb, context, TypeName);
    }

    public void EmitLazyBuilder(IndentedStringBuilder sb, IGeneratorContext context)
    {
        sb.AppendLine($"private System.Collections.Generic.List<{TypeName}> _lazyBuffer{TypeName} = new();");
        sb.AppendLine($"public void Append(System.Collections.Generic.IEnumerable<{TypeName}> dataSource) => _lazyBuffer{TypeName}.AddRange(dataSource);");
    }
    
    public void EmitLazyBuilderAppendCore(IndentedStringBuilder sb, IGeneratorContext context)
    {
        PrimaryKey.EmitLazyBuilderAppendCore(sb, context, TypeName);
    }
}

public partial class MemberMeta
{
    public static string SelectorPropertyName(string name) => $"{name.ToPascalCase()}KeySelector";
    public static string SelectorFieldName(string name) => $"_{name.ToCamelCase()}KeySelector";
    public static string ClonedIndexFieldName(string name) => $"_{name.ToCamelCase()}Index";

    public static string Comparer(ITypeSymbol symbol)
    {
        if (symbol.SpecialType is SpecialType.System_String)
        {
            return "System.StringComparer.Ordinal";
        }

        return $"System.Collections.Generic.Comparer<{symbol.ResolveType()}>.Default";
    }

    public void EmitKeySelectorDefinition(IndentedStringBuilder sb, string typeName, bool isPrimary)
    {
        var name = isPrimary ? "Primary" : Name;
        var funcType = $"Func<{typeName}, {MemberType.ResolveType()}>";
        var fName = SelectorFieldName(name);
        if (!isPrimary)
        {
            sb.AppendLine($"private readonly {typeName}[] {ClonedIndexFieldName(name)};");
        }
        else
        {
            var pName = SelectorPropertyName(name);
            sb.AppendLine($"public {funcType} {pName} => {fName};");
        }

        sb.AppendLine($"private readonly {funcType} {fName};");
    }

    public void EmitKeySelectorInitializer(IndentedStringBuilder sb, bool isPrimary)
    {
        var name = isPrimary ? "Primary" : Name;
        var fName = SelectorFieldName(name);
        sb.AppendLine($"{fName} = v => v.{Name};");
        if (!isPrimary)
        {
            sb.AppendLine($"{ClonedIndexFieldName(name)} = CloneAndSortBy({fName}, {Comparer(MemberType)});");
        }
    }

    public void Emit(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        EmitFindByKey(sb, context, typeName, isPrimary);
        sb.AppendLine();
        EmitTryFindByKey(sb, context, typeName, isPrimary);
        sb.AppendLine();
        EmitFindClosestByKey(sb, context, typeName, isPrimary);
        sb.AppendLine();
        EmitFindRangeByKey(sb, context, typeName, isPrimary);
        sb.AppendLine();
        EmitFindRangeByKeyAsSpan(sb, context, typeName, isPrimary);
        sb.AppendLine();
    }

    private void EmitFindByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        var arrayName = isPrimary ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine($"public {typeName} FindBy{Name.ToPascalCase()}({MemberType.ResolveType()} key)");
        using var _ = sb.Block();
        var intSpecialize = MemberType.SpecialType is SpecialType.System_Int32;
        var funcName = intSpecialize ? "FindUniqueCoreInt" : "FindUniqueCore";
        var selectorName = SelectorFieldName(isPrimary ? "Primary" : Name);
        var cmp = intSpecialize ? string.Empty : $", {Comparer(MemberType)}";
        var throwable = isPrimary ? ", true" : string.Empty;
        sb.AppendLine($"return {funcName}({arrayName}, key, {selectorName}{cmp}{throwable});");
    }

    private void EmitTryFindByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        var arrayName = isPrimary ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public bool TryFindBy{Name.ToPascalCase()}({MemberType.ResolveType()} key, out {typeName} result)");
        using var _ = sb.Block();
        var intSpecialize = MemberType.SpecialType is SpecialType.System_Int32;
        var funcName = intSpecialize ? "TryFindUniqueCoreInt" : "TryFindUniqueCore";
        var selectorName = SelectorFieldName(isPrimary ? "Primary" : Name);
        var cmp = intSpecialize ? string.Empty : $", {Comparer(MemberType)}";
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {selectorName}{cmp}, out result);");
    }

    private void EmitFindClosestByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        var arrayName = isPrimary ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public {typeName} FindClosestBy{Name.ToPascalCase()}({MemberType.ResolveType()} key, bool selectLower = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueClosestCore";
        var selectorName = SelectorFieldName(isPrimary ? "Primary" : Name);
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {selectorName}, {Comparer(MemberType)}, selectLower);");
    }

    private void EmitFindRangeByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        var arrayName = isPrimary ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public RangeView<{typeName}> FindRangeBy{Name.ToPascalCase()}({MemberType.ResolveType()} min, {MemberType.ResolveType()} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCore";
        var selectorName = SelectorFieldName(isPrimary ? "Primary" : Name);
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {selectorName}, {Comparer(MemberType)}, ascending);");
    }

    private void EmitFindRangeByKeyAsSpan(IndentedStringBuilder sb, IGeneratorContext context, string typeName, bool isPrimary)
    {
        var arrayName = isPrimary ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public ReadOnlySpan<{typeName}> FindRangeBy{Name.ToPascalCase()}AsSpan({MemberType.ResolveType()} min, {MemberType.ResolveType()} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCoreAsSpan";
        var selectorName = SelectorFieldName(isPrimary ? "Primary" : Name);
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {selectorName}, {Comparer(MemberType)}, ascending);");
    }

    public void EmitBuilder(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        sb.AppendLine($"public DatabaseBuilder Append(System.Collections.Generic.IEnumerable<{typeName}> dataSource)");
        using var _ = sb.Block();
        sb.AppendLine($"AppendCore(dataSource, v => v.{Name}, {Comparer(MemberType)});");
        sb.AppendLine($"return this;");
    }
    
    public void EmitLazyBuilderAppendCore(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        sb.AppendLine($"AppendCore(_lazyBuffer{typeName}, v => v.{Name}, {Comparer(MemberType)});");
    }
}

public static class EmitHelper
{
    public static void EmitFormatterRegister(this INamedTypeSymbol symbol, IndentedStringBuilder sb,
        IGeneratorContext context, ReferenceSymbols referenceSymbols)
    {
        if (context.Net7OrGreater)
        {
            sb.AppendLine($"global::MemoryPack.MemoryPackFormatterProvider.Register<{symbol}>();");
        }
        else
        {
            if (symbol.GetAttribute(referenceSymbols.MemoryPackableAttribute) is not null)
            {
                // Call generated method
                sb.AppendLine($"{symbol}.RegisterFormatter();");
            }
            else
            {
                // Manually constructed
                // Lookup RegisterFormatter method from target symbol and all interfaces
                var lookupTargets = Enumerable.Empty<INamedTypeSymbol>()
                    .Append(symbol)
                    .Concat(symbol.AllInterfaces)
                    .ToArray();
                var lookuped = false;
                foreach (var target in lookupTargets)
                {
                    var method = target.GetMembers("RegisterFormatter").FirstOrDefault();
                    if (method is not
                        {
                            IsStatic: true, Kind: SymbolKind.Method, DeclaredAccessibility: Accessibility.Public
                        }) continue;
                    // Check target class or struct or interface type is generic
                    if (target is not { IsGenericType: true } && symbol.TypeKind is TypeKind.Interface) continue;
                    if (target.IsGenericType && !target.TypeArguments.Contains(symbol, SymbolEqualityComparer.Default)) continue;
                    // Call implemented RegisterFormatter method directly
                    sb.AppendLine($"{target}.RegisterFormatter();");
                    lookuped = true;
                }
            }
        }
    }
}

public static class MemberMetaExtensions
{
    private static string GetTypeName(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas) =>
        $"({string.Join(", ", metas.Select(v => $"{v.Meta.MemberType.ResolveType()} {v.Meta.Name.ToPascalCase()}"))})";

    private static string GetMemberBaseName(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas) =>
        string.Join("_", metas.Select(v => v.Meta.Name));

    public static void EmitCombinedKeySelectorDefinition(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb,
        string typeName)
    {
        var name = metas.GetMemberBaseName();
        var tName = metas.GetTypeName();
        var funcType = $"Func<{typeName}, {tName}>";
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"private readonly {typeName}[] _{name.ToCamelCase()}Index;");
        sb.AppendLine($"private readonly {funcType} {fName};");
    }

    public static void EmitCombinedKeySelectorInitializer(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb)
    {
        var name = metas.GetMemberBaseName();
        var fName = MemberMeta.SelectorFieldName(name);
        var tName = metas.GetTypeName();
        sb.AppendLine($"{fName} = v => ({string.Join(", ", metas.Select(v => $"v.{v.Meta.Name}"))});");
        sb.AppendLine(
            $"{MemberMeta.ClonedIndexFieldName(name)} = CloneAndSortBy({fName}, System.Collections.Generic.Comparer<{tName}>.Default);");
    }

    public static void Emit(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb, string typeName)
    {
        metas.EmitFindByCombinedKey(sb, typeName);
        sb.AppendLine();
        metas.EmitTryFindByCombinedKey(sb, typeName);
        sb.AppendLine();
        metas.EmitFindClosestByCombinedKey(sb, typeName);
        sb.AppendLine();
        metas.EmitFindRangeByCombinedKey(sb, typeName);
        sb.AppendLine();
        metas.EmitFindRangeByCombinedKeyAsSpan(sb, typeName);
        sb.AppendLine();
    }

    private static void EmitFindByCombinedKey(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb, string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"public {typeName} FindBy{name}({tName} key)");
        using var _ = sb.Block();
        var funcName = "FindUniqueCore";
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {fName}, System.Collections.Generic.Comparer<{tName}>.Default);");
    }

    private static void EmitTryFindByCombinedKey(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb, string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"public bool TryFindBy{name}({tName} key, out {typeName} result)");
        using var _ = sb.Block();
        var funcName = "TryFindUniqueCore";
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, out result);");
    }

    private static void EmitFindClosestByCombinedKey(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb, string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"public {typeName} FindClosestBy{name}({tName} key, bool selectLower = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueClosestCore";
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, selectLower);");
    }

    private static void EmitFindRangeByCombinedKey(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb, string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine(
            $"public RangeView<{typeName}> FindRangeBy{name}({tName} min, {tName} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCore";
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, ascending);");
    }

    private static void EmitFindRangeByCombinedKeyAsSpan(this (MemberMeta Meta, MemberMeta.IndexInfo Info)[] metas, IndentedStringBuilder sb,
        string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine(
            $"public ReadOnlySpan<{typeName}> FindRangeBy{name}AsSpan({tName} min, {tName} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCoreAsSpan";
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, ascending);");
    }
}