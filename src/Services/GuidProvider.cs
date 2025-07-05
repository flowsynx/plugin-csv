namespace FlowSynx.Plugins.Csv.Services;

internal class GuidProvider : IGuidProvider
{
    public Guid NewGuid() => Guid.NewGuid();
}