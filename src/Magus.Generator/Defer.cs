namespace Magus.Generator;

public class Defer : IDisposable
{
    public static Defer Create(Action action)
    {
        return new Defer(action);
    }
    
    public static Defer Create()
    {
        return new Defer();
    }
    
    private List<Action> _actions;

    private Defer(Action action)
    {
        _actions = new List<Action>
        {
            action
        };
    }
    
    private Defer()
    {
        _actions = new List<Action>();
    }
    
    public Defer Add(Action action)
    {
        _actions.Add(action);
        return this;
    }
    
    public void Dispose()
    {
        foreach (var action in _actions)
        {
            action.Invoke();
        }
    }
}