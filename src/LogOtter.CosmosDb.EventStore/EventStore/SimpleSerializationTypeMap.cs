using System.Collections.Immutable;

namespace LogOtter.CosmosDb.EventStore;

internal class SimpleSerializationTypeMap
{
    private readonly ImmutableDictionary<string, string> _nameFromType;
    private readonly ImmutableDictionary<string, string> _typeFromName;

    public SimpleSerializationTypeMap(IReadOnlyCollection<Type> types)
    {
        _typeFromName = types.ToImmutableDictionary(t => t.Name, t => t.AssemblyQualifiedName!);

        _nameFromType = types.ToImmutableDictionary(t => t.AssemblyQualifiedName!, t => t.Name);
    }

    public Type GetTypeFromName(string typeName)
    {
        var fullTypeName = _typeFromName[typeName];

        return Type.GetType(fullTypeName)!;
    }

    public string GetNameFromType(Type type)
    {
        return _nameFromType[type.AssemblyQualifiedName!];
    }
}
