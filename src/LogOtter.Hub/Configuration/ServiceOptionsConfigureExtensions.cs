namespace LogOtter.Hub.Configuration;

internal static class ServiceOptionsConfigureExtensions
{
    public static ServicesOptions GetServiceOptions(this WebApplicationBuilder builder)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("services.json", optional: false)
            .AddJsonFile($"services.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables("Hub:")
            .Build();

        var serviceOptions = config.Get<ServicesOptions>();

        if (serviceOptions == null)
        {
            throw new Exception("Service options are empty");
        }

        Validate(serviceOptions);

        return serviceOptions;
    }

    private static void Validate(ServicesOptions options)
    {
        foreach (var service in options.Services)
        {
            if (string.IsNullOrWhiteSpace(service.Name))
            {
                throw new Exception("Service name cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(service.Url))
            {
                throw new Exception($"Service URL for {service.Name} cannot be empty");
            }

            if (!Uri.TryCreate(service.Url, UriKind.Absolute, out _))
            {
                throw new Exception($"Service URL for {service.Name} is not a valid URL");
            }
        }
    }
}
