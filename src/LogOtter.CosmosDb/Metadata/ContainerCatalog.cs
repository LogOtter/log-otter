namespace LogOtter.CosmosDb.Metadata;

internal class ContainerCatalog
{
    private readonly Dictionary<string, AutoProvisionMetadata> _metadata;

    public ContainerCatalog(IEnumerable<ContainerAutoProvisionMetadata> autoProvisionMetadata)
    {
        _metadata = autoProvisionMetadata.ToDictionary(m => m.ContainerName, m => m.AutoProvisionMetadata);

        Containers = _metadata.Keys;
    }

    public IEnumerable<string> Containers { get; }

    public AutoProvisionMetadata GetAutoProvisionSettings(string containerName) => _metadata[containerName];
}
