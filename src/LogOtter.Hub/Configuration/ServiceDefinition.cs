namespace LogOtter.Hub.Configuration;

public record ServiceDefinition
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}
