using Core.Contracts;
using Core.Handlers;

namespace Core.Managers;

public static class BuilderManager
{
    private static readonly Dictionary<string, INetworkBuilder> _builders = new()
    {
    };

    public static IEnumerable<string> GetAllBuilderNames()
    {
        return _builders.Keys;
    }

    private static INetworkBuilder? _activeBuilder = _builders.Values.First();
    public static INetworkBuilder GetActiveHandler()
    {
        if (_activeBuilder is null)
        {
            throw new InvalidOperationException("No active builder set.");
        }
        return _activeBuilder;
    }
    public static void SetActiveHandler(string name)
    {
        if (!_builders.TryGetValue(name, out INetworkBuilder? value))
        {
            throw new KeyNotFoundException($"Builder '{name}' not found.");
        }
        _activeBuilder = value;
    }
}
