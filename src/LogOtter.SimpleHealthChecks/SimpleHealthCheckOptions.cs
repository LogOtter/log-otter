namespace LogOtter.SimpleHealthChecks;

public record SimpleHealthCheckOptions
{
    public string Hostname { get; set; } = "+";
    public int Port { get; set; } = 80;
    public string UrlPath { get; set; } = "/health";
    public string Scheme { get; set; } = "http";
}