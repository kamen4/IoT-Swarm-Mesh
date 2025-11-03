using Core.Contracts;
using Core.Handlers;

namespace Core.Managers;

public static class HandlerManager
{
    private static readonly Dictionary<string, IHandler> _handlers = new()
    {
        {"Широковещательный", new BroadcastHandler()},
    };

    public static IEnumerable<string> GetAllHandlerNames()
    {
        return _handlers.Keys;
    }

    private static IHandler? _activeHandler = _handlers.Values.First();
    public static IHandler GetActiveHandler()
    {
        if (_activeHandler is null)
        {
            throw new InvalidOperationException("No active handler set.");
        }
        return _activeHandler;
    }
    public static void SetActiveHandler(string name)
    {
        if (!_handlers.TryGetValue(name, out IHandler? value))
        {
            throw new KeyNotFoundException($"Handler '{name}' not found.");
        }
        _activeHandler = value;
    }
}
