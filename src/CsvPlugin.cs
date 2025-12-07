using FlowSynx.PluginCore.Helpers;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Csv.Services;
using FlowSynx.Plugins.Csv.Operations.Read;
using FlowSynx.Plugins.Csv.Operations.Map;
using FlowSynx.Plugins.Csv.Operations.Filter;

namespace FlowSynx.Plugins.Csv;

public class CsvPlugin : IPlugin
{
    private IPluginLogger? _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly IReflectionGuard _reflectionGuard;
    private CsvPluginSpecifications? _specifications = null;
    private bool _isInitialized;

    public CsvPlugin() : this(new GuidProvider(), new DefaultReflectionGuard()) { }

    internal CsvPlugin(IGuidProvider guidProvider, IReflectionGuard reflectionGuard)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
        _reflectionGuard = reflectionGuard ?? throw new ArgumentNullException(nameof(reflectionGuard));
    }

    public PluginMetadata Metadata => new()
    {
        Id = Guid.Parse("81c99765-9581-4f13-ba77-86c32ae21d97"),
        Name = "Csv",
        CompanyName = "FlowSynx",
        Description = Resources.PluginDescription,
        Version = new Version(1, 3, 0),
        Category = PluginCategory.Data,
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Icon = "flowsynx.png",
        ReadMe = "README.md",
        RepositoryUrl = "https://github.com/flowsynx/plugin-csv",
        ProjectUrl = "https://flowsynx.io",
        Tags = new List<string>() { "flowSynx", "csv", "comma-separated-values", "data", "data-platform" },
        MinimumFlowSynxVersion = new Version(1, 3, 0),
    };

    public IPluginSpecifications? Specifications => _specifications;

    public IReadOnlyCollection<IPluginOperation> SupportedOperations { get; } = new IPluginOperation[]
    {
        new ReadOperation(),
        new FilterOperation(),
        new MapOperation()
    };

    public Task InitializeAsync(IPluginLogger logger, IDictionary<string, object?>? specifications)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var csvSpecifications = new CsvPluginSpecifications();
        if (specifications != null)
            csvSpecifications.FromDictionary(specifications);

        csvSpecifications.Validate();
        _specifications = csvSpecifications;

        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(
        string? operationName,
        PluginParameters parameters, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_reflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operation = SupportedOperations
            .FirstOrDefault(op => string.Equals(op.Name, operationName, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotSupportedException($"Operation '{operationName}' is not supported.");

        return operation.Name.ToLowerInvariant() switch
        {
            "read" => await ((ReadOperation)operation)
                            .ExecuteAsync(parameters.ToObject<ReadParameters>(), cancellationToken),

            "filter" => await ((FilterOperation)operation)
                            .ExecuteAsync(parameters.ToObject<FilterParameters>(), cancellationToken),

            "map" => await ((MapOperation)operation)
                            .ExecuteAsync(parameters.ToObject<MapParameters>(), cancellationToken),

            _ => throw new InvalidOperationException($"Unsupported operation: {operation.Name}")
        };
    }
}