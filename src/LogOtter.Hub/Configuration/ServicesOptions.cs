namespace LogOtter.Hub.Configuration;

public record ServicesOptions
{
    public IEnumerable<ServiceDefinition> Services { get; set; } = new List<ServiceDefinition>();
}
