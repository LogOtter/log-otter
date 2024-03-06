using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerApi.HealthChecks;

public class ResolveAllControllersHealthCheck : IHealthCheck
{
    private readonly IList<Type> _controllerTypes;
    private readonly IServiceProvider _serviceProvider;

    public ResolveAllControllersHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _controllerTypes = typeof(ResolveAllControllersHealthCheck)
            .Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failures = new List<(Type Type, string ExceptionMessage)>();
        var successes = new List<(Type Type, List<Type> ParametersResolved)>();

        foreach (var controllerType in _controllerTypes)
        {
            try
            {
                var parameterTypes = ResolveController(controllerType);
                successes.Add((controllerType, parameterTypes));
            }
            catch (Exception ex)
            {
                failures.Add((controllerType, ex.Message));
            }
        }

        if (failures.Any())
        {
            var failureMessage = string.Join(
                Environment.NewLine,
                failures.Select(controller => $"Failed to resolve {controller.Type.Name}: {controller.ExceptionMessage}")
            );

            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, failureMessage));
        }

        var successMessage = string.Join(
            Environment.NewLine,
            successes.Select(controller => $"Resolved {controller.Type} with parameters: {string.Join(", ", controller.ParametersResolved)}")
        );

        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, successMessage));
    }

    private List<Type> ResolveController(Type controllerType)
    {
        var parameterValues = new List<Type>();

        var parameterTypes = controllerType.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).Distinct();

        foreach (var parameterType in parameterTypes)
        {
            var resolved = _serviceProvider.GetRequiredService(parameterType);
            parameterValues.Add(resolved.GetType());
        }

        return parameterValues;
    }
}
