using System.Runtime.CompilerServices;
using System.Text;

namespace Magus.Generator;

public class IndentedStringBuilder
{
    private const string Tab = "\t";
    private const string Space4 = "    ";
    private const string Space2 = "  ";

    public enum Mode
    {
        Tab,
        Space4,
        Space2
    }

    private readonly Mode _mode;
    private readonly StringBuilder _builder;
    private string _completeIndentation = "";
    private int _indent;
    public string Pfx => _completeIndentation;

    public IndentedStringBuilder(Mode mode = Mode.Tab)
    {
        _mode = mode;
        _builder = new();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine() => _builder.AppendLine();

    public void AppendLine(string value)
    {
        _builder.AppendLine(_completeIndentation + value);
    }

    public void AppendLine(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    private void UpdateInternal()
    {
        _completeIndentation = string.Empty;
        var t = _mode switch
        {
            Mode.Tab => Tab,
            Mode.Space4 => Space4,
            Mode.Space2 => Space2,
            _ => throw new ArgumentOutOfRangeException()
        };
        _completeIndentation = Enumerable.Range(0, _indent).Aggregate(string.Empty, (acc, _) => acc + t);
    }

    public IDisposable Increase()
    {
        return Increase(string.Empty);
    }

    /// <summary>
    /// 先頭行にのみprefixをつける段落を作成
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public IDisposable Increase(string prefix)
    {
        _indent++;
        UpdateInternal();
        return Disposable.Create(Decrease);
    }

    public void Decrease()
    {
        _indent--;
        UpdateInternal();
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
    
    private class Disposable : IDisposable
    {
        private readonly Action _action;
        
        private Disposable(Action action)
        {
            _action = action;
        }
        
        public static Disposable Create(Action action)
        {
            return new Disposable(action);
        }

        public void Dispose()
        {
            _action.Invoke();
        }
    }

}

public static class IndentedStringBuilderExtensions
{
    public static IDisposable Block(this IndentedStringBuilder sb)
    {
        sb.AppendLine("{");
        sb.Increase();
        return Defer.Create(() =>
        {
            sb.Decrease();
            sb.AppendLine("}");
        });
    }
    
    public static IDisposable BlockStmt(this IndentedStringBuilder sb)
    {
        sb.AppendLine("{");
        sb.Increase();
        return Defer.Create(() =>
        {
            sb.Decrease();
            sb.AppendLine("};");
        });
    }
}
