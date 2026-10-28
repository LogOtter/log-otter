using LogOtter.CosmosDb.Metadata;

namespace LogOtter.CosmosDb.Testing;

internal static class PreProvisionedContainerValidator
{
    public static void Validate(
        IEnumerable<PreProvisionedContainer> preProvisionedContainers,
        IEnumerable<ContainerAutoProvisionMetadata> registeredContainers,
        bool autoProvisioningEnabled
    )
    {
        var preProvisioned = preProvisionedContainers.Select(c => (c.ContainerName, c.DocumentType)).Distinct().ToList();
        var registered = registeredContainers.ToList();
        var registeredContainersByName = registered.ToDictionary(c => c.ContainerName);
        var registeredContainersByType = registered
            .SelectMany(c => DocumentTypes(c).Select(t => (DocumentType: t, Container: c)))
            .ToDictionary(r => r.DocumentType, r => r.Container);

        var errors = new List<string>();

        foreach (var (containerName, documentType) in preProvisioned)
        {
            registeredContainersByType.TryGetValue(documentType, out var typeRegistration);
            registeredContainersByName.TryGetValue(containerName, out var containerRegistration);

            if (typeRegistration != null && typeRegistration.ContainerName != containerName)
            {
                var error =
                    $"{documentType.Name} is pre-provisioned in container {containerName} in the test setup, but the live config registers it with container {typeRegistration.ContainerName}";

                if (containerRegistration != null)
                {
                    error += $" (and registers container {containerName} for {FormatTypes(containerRegistration)})";
                }

                errors.Add(error);
            }
            else if (typeRegistration == null && containerRegistration != null)
            {
                var baseType = containerRegistration.DocumentType;

                errors.Add(
                    baseType.IsAssignableFrom(documentType)
                        ? $"{documentType.Name} is pre-provisioned in container {containerName} in the test setup, but the live config does not register it as a subtype of {baseType.Name} (use WithSubTypeThatMustHaveDistinctPartition<{documentType.Name}>() to register it)"
                        : $"Container {containerName} is pre-provisioned for {documentType.Name} in the test setup, but the live config registers it for {FormatTypes(containerRegistration)}"
                );
            }
        }

        var preProvisionedContainerNames = preProvisioned.Select(c => c.ContainerName).ToHashSet();

        foreach (var container in registered)
        {
            var isPreProvisioned = preProvisionedContainerNames.Contains(container.ContainerName);

            if (autoProvisioningEnabled && isPreProvisioned)
            {
                errors.Add(
                    $"Container {container.ContainerName} is pre-provisioned in the test setup, but the live config uses WithAutoProvisioning() so it will be created automatically. Either remove the pre-provisioning from the test setup, or remove WithAutoProvisioning() from the live config if the container should already exist"
                );
            }
            else if (!autoProvisioningEnabled && !isPreProvisioned)
            {
                errors.Add(
                    $"Container {container.ContainerName} is not pre-provisioned in the test setup, but the live config does not use WithAutoProvisioning() so the container must already exist. Either pre-provision it in the test setup using WithPreProvisionedContainer<{container.DocumentType.Name}>(\"{container.ContainerName}\") with the same settings as the real container, or add WithAutoProvisioning() to the live config to create it automatically"
                );
            }
        }

        if (errors.Count > 0)
        {
            var errorLines = string.Join(Environment.NewLine, errors.Select(e => $" - {e}"));
            throw new InvalidOperationException(
                $"The containers pre-provisioned in the test setup do not match the live config:{Environment.NewLine}{errorLines}"
            );
        }
    }

    private static IEnumerable<Type> DocumentTypes(ContainerAutoProvisionMetadata container) => container.SubTypes.Prepend(container.DocumentType);

    private static string FormatTypes(ContainerAutoProvisionMetadata container) => string.Join(", ", DocumentTypes(container).Select(t => t.Name));
}
