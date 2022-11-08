using System.Collections.Immutable;

namespace LogOtter.CosmosDb.EventStore;

public class SimpleSerializationTypeMap : ISerializationTypeMap
{
    private readonly ImmutableDictionary<string, string> _typeFromName;
    private readonly ImmutableDictionary<string, string> _nameFromType;

    public SimpleSerializationTypeMap(IReadOnlyCollection<Type> types)
    {
        _typeFromName = types
            .ToImmutableDictionary(t => t.Name, t => t.AssemblyQualifiedName!);

        _nameFromType = types
            .ToImmutableDictionary(t => t.AssemblyQualifiedName!, t => t.Name);
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