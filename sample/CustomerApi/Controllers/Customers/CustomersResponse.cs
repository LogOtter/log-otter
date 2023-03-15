using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LogOtter.JsonHal;

namespace CustomerApi.Controllers.Customers;

public class CustomersResponse
{
    public required IReadOnlyCollection<CustomerResponse> Customers { get; init; }

    [JsonPropertyName("_links")]
    public required JsonHalLinkCollection Links { get; init; }

    [SetsRequiredMembers]
    public CustomersResponse(IReadOnlyCollection<CustomerResponse> customers)
    {
        Customers = customers;
        Links = new JsonHalLinkCollection();
    }

    public CustomersResponse()
    {
    }
}
