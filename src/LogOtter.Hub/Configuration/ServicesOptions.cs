namespace LogOtter.Hub.Configuration;

public class ServicesOptions
{
    public IEnumerable<ServiceDefinition> Services { get; set; } = new List<ServiceDefinition>();
}
