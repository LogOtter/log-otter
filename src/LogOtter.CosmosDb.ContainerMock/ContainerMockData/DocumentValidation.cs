using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace LogOtter.CosmosDb.ContainerMock.ContainerMockData;

internal static class DocumentValidation
{
    private const string WrongPartitionKeyMessage =
        "PartitionKey extracted from document doesn't match the one specified in the header. Learn more: https://aka.ms/CosmosDB/sql/errors/wrong-pk-value";

    public static void EnsureHasId(JObject document, string containerName, Type? documentType)
    {
        if (document["id"] is null or { Type: JTokenType.Null })
        {
            throw new ContainerMockException(
                HttpStatusCode.BadRequest,
                $"Cosmos DB requires documents to have an 'id' property, but {DescribeDocument(documentType)} being written to container {containerName} doesn't have one. {PropertyNameHint(document, "/id", documentType)}"
            );
        }
    }

    public static void EnsurePartitionKeyMatches(
        JObject document,
        string partitionKeyPath,
        PartitionKey partitionKey,
        string containerName,
        Type? documentType
    )
    {
        var documentValue = document.SelectToken(ToJsonPath(partitionKeyPath));
        var documentPartitionKey = ToPartitionKey(documentValue);

        if (documentPartitionKey.Equals(partitionKey))
        {
            return;
        }

        var hint =
            documentValue == null
                ? $"Container {containerName} is partitioned by {partitionKeyPath}, but {DescribeDocument(documentType)} doesn't have that property. {PropertyNameHint(document, partitionKeyPath, documentType)}"
                : $"Container {containerName} is partitioned by {partitionKeyPath}, but {DescribeDocument(documentType)} has the value {documentPartitionKey} there, which doesn't match the partition key passed in ({partitionKey}).";

        throw new ContainerMockException(HttpStatusCode.BadRequest, $"{WrongPartitionKeyMessage}. {hint}");
    }

    public static void EnsureHasPartitionKeyProperty(JObject document, string partitionKeyPath, string containerName, Type? documentType)
    {
        if (document.SelectToken(ToJsonPath(partitionKeyPath)) == null)
        {
            throw new ArgumentException(
                $"Could not extract the partition key from {DescribeDocument(documentType)}, as container {containerName} is partitioned by {partitionKeyPath} but the document doesn't have that property. {PropertyNameHint(document, partitionKeyPath, documentType)}"
            );
        }
    }

    private static string DescribeDocument(Type? documentType)
    {
        var typeName = documentType == null || documentType.IsDefined(typeof(CompilerGeneratedAttribute), false) ? null : documentType.Name;
        return typeName != null ? $"the {typeName} document" : "the document";
    }

    private static string PropertyNameHint(JObject document, string path, Type? documentType)
    {
        var propertyName = path.Split('/').Last();
        var isTopLevel = path.Count(c => c == '/') == 1;
        var differentlyCased = isTopLevel
            ? document
                .Properties()
                .FirstOrDefault(p => p.Name != propertyName && string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            : null;

        return differentlyCased != null
            ? $"The document has a property called '{differentlyCased.Name}', but property names are case sensitive, so make sure it's serialized as '{propertyName}', e.g. with [JsonProperty(\"{propertyName}\")]."
            : $"Make sure {DescribeDocument(documentType)} has a property serialized as '{propertyName}', e.g. with [JsonProperty(\"{propertyName}\")].";
    }

    private static string ToJsonPath(string path) => path.Substring(1).Replace('/', '.');

    private static PartitionKey ToPartitionKey(JToken? token)
    {
        return token?.Type switch
        {
            null => PartitionKey.None,
            JTokenType.Null => PartitionKey.Null,
            JTokenType.String => new PartitionKey(token.Value<string>()),
            JTokenType.Integer or JTokenType.Float => new PartitionKey(token.Value<double>()),
            JTokenType.Boolean => new PartitionKey(token.Value<bool>()),
            _ => PartitionKey.None,
        };
    }
}
