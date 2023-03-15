using System.Reflection;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

public static class PartitionKeyHelpers
{
    private static readonly MethodInfo TryParseJsonStringMethod;

    static PartitionKeyHelpers()
    {
        var partitionKeyType = typeof(PartitionKey);
        TryParseJsonStringMethod = partitionKeyType.GetMethod("TryParseJsonString", BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    public static PartitionKey FromJsonString(string partitionKeyAsJson)
    {
        var parameters = new object?[] { partitionKeyAsJson, null };
        var result = TryParseJsonStringMethod.Invoke(null, parameters);

        if (result is true)
        {
            return (PartitionKey)parameters[1]!;
        }

        throw new Exception("Could not parse partition key");
    }
}
