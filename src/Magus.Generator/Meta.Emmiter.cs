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
        PrimaryKey.EmitKeySelectorDefinition(sb, TypeName);
        foreach (var index in Indexes)
        {
            index.EmitKeySelectorDefinition(sb, TypeName);
        }

        foreach (var combinedIndex in CombinedIndexes)
        {
            combinedIndex.EmitCombinedKeySelectorDefinition(sb, TypeName);
        }

        sb.AppendLine();
        EmitConstructor(sb, context);
        sb.AppendLine();

        PrimaryKey.Emit(sb, context, TypeName);
        foreach (var index in Indexes)
        {
            index.Emit(sb, context, TypeName);
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
            PrimaryKey.EmitKeySelectorInitializer(sb);
            foreach (var index in Indexes)
            {
                index.EmitKeySelectorInitializer(sb);
            }

            foreach (var combinedIndex in CombinedIndexes)
            {
                combinedIndex.EmitCombinedKeySelectorInitializer(sb);
            }
        }
    }

    public void EmitFormatterRegister(IndentedStringBuilder sb, IGeneratorContext context)
    {
        if (IsUnmanagedType)
        {
            sb.AppendLine($"{TypeName}.RegisterFormatter();");
        }

        foreach (var member in Members)
        {
            member.EmitFormatterRegister(sb, context);
        }
    }

    public void EmitBuilder(IndentedStringBuilder sb, IGeneratorContext context)
    {
        PrimaryKey.EmitBuilder(sb, context, TypeName);
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

    public void EmitKeySelectorDefinition(IndentedStringBuilder sb, string typeName)
    {
        EmitKeySelectorDefinition(sb, typeName, IsPrimaryKey);
    }

    private void EmitKeySelectorDefinition(IndentedStringBuilder sb, string typeName, bool primary)
    {
        var name = primary ? "Primary" : Name;
        var funcType = $"Func<{typeName}, {MemberType.ResolveType()}>";
        var fName = SelectorFieldName(name);
        if (!primary)
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

    public void EmitKeySelectorInitializer(IndentedStringBuilder sb)
    {
        var name = IsPrimaryKey ? "Primary" : Name;
        var fName = SelectorFieldName(name);
        sb.AppendLine($"{fName} = v => v.{Name};");
        if (!IsPrimaryKey)
        {
            sb.AppendLine($"{ClonedIndexFieldName(name)} = CloneAndSortBy({fName}, {Comparer(MemberType)});");
        }
    }

    public void Emit(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        EmitFindByKey(sb, context, typeName);
        sb.AppendLine();
        EmitTryFindByKey(sb, context, typeName);
        sb.AppendLine();
        EmitFindClosestByKey(sb, context, typeName);
        sb.AppendLine();
        EmitFindRangeByKey(sb, context, typeName);
        sb.AppendLine();
        EmitFindRangeByKeyAsSpan(sb, context, typeName);
        sb.AppendLine();
    }

    private void EmitFindByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        var arrayName = IsPrimaryKey ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine($"public {typeName} FindBy{Name.ToPascalCase()}({MemberType.ResolveType()} key)");
        using var _ = sb.Block();
        var intSpecialize = MemberType.SpecialType is SpecialType.System_Int32;
        var funcName = intSpecialize ? "FindUniqueCoreInt" : "FindUniqueCore";
        var selectorName = SelectorFieldName(IsPrimaryKey ? "Primary" : Name);
        var cmp = intSpecialize ? string.Empty : $", {Comparer(MemberType)}";
        var throwable = IsPrimaryKey ? ", true" : string.Empty;
        sb.AppendLine($"return {funcName}({arrayName}, key, {selectorName}{cmp}{throwable});");
    }

    private void EmitTryFindByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        var arrayName = IsPrimaryKey ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public bool TryFindBy{Name.ToPascalCase()}({MemberType.ResolveType()} key, out {typeName} result)");
        using var _ = sb.Block();
        var intSpecialize = MemberType.SpecialType is SpecialType.System_Int32;
        var funcName = intSpecialize ? "TryFindUniqueCoreInt" : "TryFindUniqueCore";
        var selectorName = SelectorFieldName(IsPrimaryKey ? "Primary" : Name);
        var cmp = intSpecialize ? string.Empty : $", {Comparer(MemberType)}";
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {selectorName}{cmp}, out result);");
    }

    private void EmitFindClosestByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        var arrayName = IsPrimaryKey ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public {typeName} FindClosestBy{Name.ToPascalCase()}({MemberType.ResolveType()} key, bool selectLower = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueClosestCore";
        var selectorName = SelectorFieldName(IsPrimaryKey ? "Primary" : Name);
        sb.AppendLine(
            $"return {funcName}({arrayName}, key, {selectorName}, {Comparer(MemberType)}, selectLower);");
    }

    private void EmitFindRangeByKey(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        var arrayName = IsPrimaryKey ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public RangeView<{typeName}> FindRangeBy{Name.ToPascalCase()}({MemberType.ResolveType()} min, {MemberType.ResolveType()} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCore";
        var selectorName = SelectorFieldName(IsPrimaryKey ? "Primary" : Name);
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {selectorName}, {Comparer(MemberType)}, ascending);");
    }

    private void EmitFindRangeByKeyAsSpan(IndentedStringBuilder sb, IGeneratorContext context, string typeName)
    {
        var arrayName = IsPrimaryKey ? "Values" : ClonedIndexFieldName(Name);
        sb.AppendLine(
            $"public ReadOnlySpan<{typeName}> FindRangeBy{Name.ToPascalCase()}AsSpan({MemberType.ResolveType()} min, {MemberType.ResolveType()} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCoreAsSpan";
        var selectorName = SelectorFieldName(IsPrimaryKey ? "Primary" : Name);
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

    public void EmitFormatterRegister(IndentedStringBuilder sb, IGeneratorContext context)
    {
        if (!MemberType.IsUnmanagedType || MemberType.SpecialType is not SpecialType.None) return;
        sb.AppendLine($"{MemberType}.RegisterFormatter();");
    }
}

public static class MemberMetaExtensions
{
    private static string GetTypeName(this MemberMeta[] metas) =>
        $"({string.Join(", ", metas.Select(v => $"{v.MemberType.ResolveType()} {v.Name.ToPascalCase()}"))})";

    private static string GetMemberBaseName(this MemberMeta[] metas) =>
        string.Join("_", metas.Select(v => v.Name));

    public static void EmitCombinedKeySelectorDefinition(this MemberMeta[] metas, IndentedStringBuilder sb,
        string typeName)
    {
        var name = metas.GetMemberBaseName();
        var tName = metas.GetTypeName();
        var funcType = $"Func<{typeName}, {tName}>";
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"private readonly {typeName}[] _{name.ToCamelCase()}Index;");
        sb.AppendLine($"private readonly {funcType} {fName};");
    }

    public static void EmitCombinedKeySelectorInitializer(this MemberMeta[] metas, IndentedStringBuilder sb)
    {
        var name = metas.GetMemberBaseName();
        var fName = MemberMeta.SelectorFieldName(name);
        var tName = metas.GetTypeName();
        sb.AppendLine($"{fName} = v => ({string.Join(", ", metas.Select(v => $"v.{v.Name}"))});");
        sb.AppendLine(
            $"{MemberMeta.ClonedIndexFieldName(name)} = CloneAndSortBy({fName}, System.Collections.Generic.Comparer<{tName}>.Default);");
    }

    public static void Emit(this MemberMeta[] metas, IndentedStringBuilder sb, string typeName)
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

    private static void EmitFindByCombinedKey(this MemberMeta[] metas, IndentedStringBuilder sb, string typeName)
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

    private static void EmitTryFindByCombinedKey(this MemberMeta[] metas, IndentedStringBuilder sb, string typeName)
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

    private static void EmitFindClosestByCombinedKey(this MemberMeta[] metas, IndentedStringBuilder sb, string typeName)
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

    private static void EmitFindRangeByCombinedKey(this MemberMeta[] metas, IndentedStringBuilder sb, string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"public RangeView<{typeName}> FindRangeBy{name}({tName} min, {tName} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCore";
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, ascending);");
    }

    private static void EmitFindRangeByCombinedKeyAsSpan(this MemberMeta[] metas, IndentedStringBuilder sb,
        string typeName)
    {
        var name = metas.GetMemberBaseName();
        var arrayName = $"_{name.ToCamelCase()}Index";
        var tName = metas.GetTypeName();
        var fName = MemberMeta.SelectorFieldName(name);
        sb.AppendLine($"public ReadOnlySpan<{typeName}> FindRangeBy{name}AsSpan({tName} min, {tName} max, bool ascending = true)");
        using var _ = sb.Block();
        var funcName = "FindUniqueRangeCoreAsSpan";
        sb.AppendLine(
            $"return {funcName}({arrayName}, min, max, {fName}, System.Collections.Generic.Comparer<{tName}>.Default, ascending);");
    }
}