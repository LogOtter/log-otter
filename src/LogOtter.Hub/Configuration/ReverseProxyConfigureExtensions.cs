using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace LogOtter.Hub.Configuration;

internal static class ReverseProxyConfigureExtensions
{
    public static void Initialize(this IReverseProxyBuilder reverseProxyBuilder)
    {
        reverseProxyBuilder.LoadFromMemory(new List<RouteConfig>(), new List<ClusterConfig>());
    }

    public static void ConfigureReverseProxy(this WebApplication webApplication, ServicesOptions servicesOptions)
    {
        var inMemoryConfigProvider = webApplication.Services.GetRequiredService<InMemoryConfigProvider>();

        var clusters = CreateClusters(servicesOptions);
        var routes = CreateRoutes(servicesOptions);

        inMemoryConfigProvider.Update(routes, clusters);
    }

    private static IReadOnlyList<ClusterConfig> CreateClusters(ServicesOptions servicesOptions)
    {
        var clusters = new List<ClusterConfig>();

        foreach (var service in servicesOptions.Services)
        {
            var clusterConfig = new ClusterConfig
            {
                ClusterId = service.Name,
                Destinations = new Dictionary<string, DestinationConfig> { [service.Name] = new() { Address = service.Url } }
            };

            clusters.Add(clusterConfig);
        }

        return clusters;
    }

    private static IReadOnlyList<RouteConfig> CreateRoutes(ServicesOptions servicesOptions)
    {
        var routes = new List<RouteConfig>();

        foreach (var service in servicesOptions.Services)
        {
            var prefix = $"/api/service/{service.Name}";

            var routeConfig = CreateRouteConfig(service, prefix)
                .WithTransformPathRemovePrefix(prefix)
                .WithTransformRequestHeader("X-LogOtter-Hub-Path", prefix);

            routes.Add(routeConfig);
        }

        return routes;
    }

    private static RouteConfig CreateRouteConfig(ServiceDefinition service, string prefix)
    {
        return new RouteConfig
        {
            RouteId = service.Name,
            ClusterId = service.Name,
            Match = new RouteMatch { Path = $"{prefix}/{{**catch-all}}" }
        };
    }
}
