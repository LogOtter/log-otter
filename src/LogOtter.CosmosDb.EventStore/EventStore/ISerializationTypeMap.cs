namespace LogOtter.CosmosDb.EventStore;

public interface ISerializationTypeMap
{
    Type GetTypeFromName(string typeName);

    string GetNameFromType(Type type);
}
