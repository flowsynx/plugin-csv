using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Csv.Services;

internal interface ICsvManger
{
    Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken);
    Task Write(PluginParameters parameters, CancellationToken cancellationToken);
}