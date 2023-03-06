namespace LogOtter.CosmosDb;

public record ManagedIdentityOptions
{
    public string AccountEndpoint { get; set; } = "";
    public string? UserAssignedManagedIdentityClientId { get; set; }
}
