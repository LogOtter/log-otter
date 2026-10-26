using LogOtter.CosmosDb.Metadata;

namespace LogOtter.CosmosDb.Testing;

internal static class PreProvisionedContainerValidator
{
    public static void Validate(
        IEnumerable<PreProvisionedContainer> preProvisionedContainers,
        IEnumerable<ContainerAutoProvisionMetadata> registeredContainers
    )
    {
        var registered = registeredContainers.ToList();
        var registeredContainersByName = registered.ToDictionary(c => c.ContainerName);
        var registeredContainersByType = registered
            .SelectMany(c => DocumentTypes(c).Select(t => (DocumentType: t, Container: c)))
            .ToDictionary(r => r.DocumentType, r => r.Container);

        var errors = new List<string>();

        foreach (var (containerName, documentType) in preProvisionedContainers.Distinct())
        {
            registeredContainersByType.TryGetValue(documentType, out var typeRegistration);
            registeredContainersByName.TryGetValue(containerName, out var containerRegistration);

            if (typeRegistration != null && typeRegistration.ContainerName != containerName)
            {
                var error =
                    $"{documentType.Name} is pre-provisioned in container {containerName} but is registered with container {typeRegistration.ContainerName}";

                if (containerRegistration != null)
                {
                    error += $" (container {containerName} is registered for {FormatTypes(containerRegistration)})";
                }

                errors.Add(error);
            }
            else if (typeRegistration == null && containerRegistration != null)
            {
                var baseType = containerRegistration.DocumentType;

                errors.Add(
                    baseType.IsAssignableFrom(documentType)
                        ? $"{documentType.Name} is pre-provisioned in container {containerName} but is not registered as a subtype of {baseType.Name} (use WithSubTypeThatMustHaveDistinctPartition<{documentType.Name}>() to register it)"
                        : $"Container {containerName} is pre-provisioned for {documentType.Name} but is registered for {FormatTypes(containerRegistration)}"
                );
            }
        }

        if (errors.Count > 0)
        {
            var errorLines = string.Join(Environment.NewLine, errors.Select(e => $" - {e}"));
            throw new InvalidOperationException(
                $"Pre-provisioned containers do not match the registered containers:{Environment.NewLine}{errorLines}"
            );
        }
    }

    private static IEnumerable<Type> DocumentTypes(ContainerAutoProvisionMetadata container) => container.SubTypes.Prepend(container.DocumentType);

    private static string FormatTypes(ContainerAutoProvisionMetadata container) => string.Join(", ", DocumentTypes(container).Select(t => t.Name));
}
