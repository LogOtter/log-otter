namespace LogOtter.EventStore.CosmosDb;

public interface ISerializationTypeMap
{
    Type GetTypeFromName(string typeName);

    string GetNameFromType(Type type);
}