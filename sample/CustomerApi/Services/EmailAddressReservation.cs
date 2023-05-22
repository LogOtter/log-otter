using Newtonsoft.Json;

namespace CustomerApi.Services;

public class EmailAddressReservation
{
    public static readonly string StaticPartitionKey = "EmailAddress";

    [JsonProperty("id")]
    public string Id => EmailAddress.ToUpper();

    [JsonProperty("partitionKey")]
    public string PartitionKey => StaticPartitionKey;
    public string EmailAddress { get; set; }

    public EmailAddressReservation(string emailAddress)
    {
        EmailAddress = emailAddress;
    }
}
