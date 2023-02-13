namespace LogOtter.SimpleHealthChecks;

public record SimpleHealthCheckHostOptions
{
    public string Hostname { get; set; } = "+";
    public int Port { get; set; } = 80;
    public string Scheme { get; set; } = "http";
}
