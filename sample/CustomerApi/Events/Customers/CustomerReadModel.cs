using CustomerApi.Uris;
using LogOtter.CosmosDb.EventStore;
using Newtonsoft.Json;

namespace CustomerApi.Events.Customers;

#pragma warning disable CS8618

public class CustomerReadModel : ISnapshot
{
    public const string StaticPartitionKey = "customers";

    public CustomerUri CustomerUri { get; set; }

    public string EmailAddress { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    [JsonProperty("id")]
    public string Id { get; init; }

    [JsonProperty("partitionKey")]
    public string PartitionKey => StaticPartitionKey;

    public DateTimeOffset? DeletedAt { get; set; }

    //TODO: CreatedBy?

    public int Revision { get; set; }
}
