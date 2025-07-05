using FlowSynx.PluginCore.Helpers;

namespace FlowSynx.Plugins.Csv.Services;

internal class DefaultReflectionGuard : IReflectionGuard
{
    public bool IsCalledViaReflection() => ReflectionHelper.IsCalledViaReflection();
}