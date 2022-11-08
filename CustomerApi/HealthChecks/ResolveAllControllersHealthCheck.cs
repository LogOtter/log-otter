using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CustomerApi.HealthChecks;

public class ResolveAllControllersHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<Type> _controllerTypes;

    public ResolveAllControllersHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _controllerTypes = typeof(ResolveAllControllersHealthCheck)
            .Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<(Type Type, string Message)>();
        var successes = new List<(Type Type, List<Type> ParametersResolved)>();

        foreach (var controllerType in _controllerTypes)
        {
            var constructors = controllerType.GetConstructors();
            try
            {
                var parameterValues = new List<Type>();
                foreach (var parameter in constructors.SelectMany(c => c.GetParameters().Distinct()))
                {
                    var parameterValue = _serviceProvider.GetRequiredService(parameter.ParameterType);
                    parameterValues.Add(parameterValue.GetType());
                }

                successes.Add((controllerType, parameterValues));
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
                failures.Select(f => $"Failed to resolve {f.Type.Name}: {f.Message}")
            );

            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, failureMessage));
        }

        var successMessage = string.Join(
            Environment.NewLine,
            successes.Select(t => $"Resolved {t.Type} with parameters: {string.Join(", ", t.ParametersResolved)}")
        );
        
        return Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, successMessage));
    }
}