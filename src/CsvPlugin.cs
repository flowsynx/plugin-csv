using FlowSynx.PluginCore.Helpers;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Csv.Services;

namespace FlowSynx.Plugins.Csv;

public class CsvPlugin: IPlugin
{
    private IPluginLogger? _logger;
    private ICsvManger _manager = null!;
    private CsvPluginSpecifications _csvSenderSpecifications = null!;
    private bool _isInitialized;

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("81c99765-9581-4f13-ba77-86c32ae21d97"),
                Name = "Csv",
                CompanyName = "FlowSynx",
                Description = Resources.PluginDescription,
                Version = new PluginVersion(1, 0, 0),
                Namespace = PluginNamespace.Connectors,
                Authors = new List<string> { "FlowSynx" },
                Copyright = "© FlowSynx. All rights reserved.",
                Icon = "flowsynx.png",
                ReadMe = "README.md",
                RepositoryUrl = "https://github.com/flowsynx/plugin-csv",
                ProjectUrl = "https://flowsynx.io",
                Tags = new List<string>() { "flowSynx", "csv", "comma-separated-values)", "data-platform", "bi-plugins" },
                Category = PluginCategories.DataPlatformAndBI
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(CsvPluginSpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        ArgumentNullException.ThrowIfNull(logger);
        _csvSenderSpecifications = Specifications.ToObject<CsvPluginSpecifications>();
        _logger = logger;
        _manager = new CsvManager(logger);
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        if (OperationMap.TryGetValue(operation, out var handler))
        {
            return handler(parameters, cancellationToken);
        }

        throw new NotSupportedException($"CSV plugin: Operation '{operation}' is not supported.");
    }

    private Dictionary<string, Func<PluginParameters, CancellationToken, Task<object?>>> OperationMap => new(StringComparer.OrdinalIgnoreCase)
    {
        ["read"] = async (parameters, cancellationToken) => await _manager.Read(parameters, cancellationToken),
        ["write"] = async (parameters, cancellationToken) => { await _manager.Write(parameters, cancellationToken); return null; },
    };

    public IReadOnlyCollection<string> SupportedOperations => OperationMap.Keys;
}