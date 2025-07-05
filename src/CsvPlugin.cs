using FlowSynx.PluginCore.Helpers;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Models;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Csv.Services;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using System.Dynamic;

namespace FlowSynx.Plugins.Csv;

public class CsvPlugin: IPlugin
{
    private IPluginLogger? _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly IReflectionGuard _reflectionGuard;
    private CsvPluginSpecifications _csvSenderSpecifications = null!;
    private bool _isInitialized;

    public CsvPlugin() : this(new GuidProvider(), new DefaultReflectionGuard()) { }

    internal CsvPlugin(IGuidProvider guidProvider, IReflectionGuard reflectionGuard)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
        _reflectionGuard = reflectionGuard ?? throw new ArgumentNullException(nameof(reflectionGuard));
    }

    public PluginMetadata Metadata => new PluginMetadata
    {
        Id = Guid.Parse("81c99765-9581-4f13-ba77-86c32ae21d97"),
        Name = "Csv",
        CompanyName = "FlowSynx",
        Description = Resources.PluginDescription,
        Version = new PluginVersion(1, 1, 0),
        Category = PluginCategory.Data,
        Authors = new List<string> { "FlowSynx" },
        Copyright = "© FlowSynx. All rights reserved.",
        Icon = "flowsynx.png",
        ReadMe = "README.md",
        RepositoryUrl = "https://github.com/flowsynx/plugin-csv",
        ProjectUrl = "https://flowsynx.io",
        Tags = new List<string>() { "flowSynx", "csv", "comma-separated-values", "data", "data-platform" },
    };

    public PluginSpecifications? Specifications { get; set; }

    public Type SpecificationsType => typeof(CsvPluginSpecifications);

    private Dictionary<string, ICsvOperationHandler> OperationMap => new(StringComparer.OrdinalIgnoreCase)
    {
        ["filter"] = new FilterOperationHandler(),
        ["map"] = new MapOperationHandler()
    };

    public IReadOnlyCollection<string> SupportedOperations => OperationMap.Keys;

    public Task Initialize(IPluginLogger logger)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        ArgumentNullException.ThrowIfNull(logger);
        _csvSenderSpecifications = Specifications.ToObject<CsvPluginSpecifications>();
        _logger = logger;
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_reflectionGuard.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var inputParameter = parameters.ToObject<InputParameter>();
        if (!OperationMap.TryGetValue(inputParameter.Operation, out var handler))
        {
            throw new NotSupportedException($"Operation '{inputParameter.Operation}' is not supported.");
        }

        var context = ParseDataToContext(inputParameter.Data);
        var csv = context.Content ?? throw new ArgumentException("Input CSV is required.");

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = inputParameter.Delimiter ?? ",",
            IgnoreBlankLines = inputParameter.IgnoreBlankLines ?? true,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = true,
            BadDataFound = null
        });

        var records = csvReader.GetRecords<dynamic>().Select(row =>
        {
            var expando = new ExpandoObject() as IDictionary<string, object?>;
            foreach (var kvp in (IDictionary<string, object?>)row)
            {
                expando[kvp.Key] = kvp.Value;
            }
            return (ExpandoObject)expando;
        }).ToList();

        var result = handler.Handle(records, inputParameter);
        var csvString = await ToCsvStringAsync(result, inputParameter);

        string filename = $"{_guidProvider.NewGuid()}.csv";
        return new PluginContext(filename, "Data")
        {
            Format = "Csv",
            Content = csvString
        };
    }

    private PluginContext ParseDataToContext(object? data)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data), "Input data cannot be null.");

        return data switch
        {
            PluginContext singleContext => singleContext,
            IEnumerable<PluginContext> => throw new NotSupportedException("List of PluginContext is not supported."),
            string strData => new PluginContext(_guidProvider.NewGuid().ToString(), "Data") { Content = strData },
            _ => throw new NotSupportedException("Unsupported input data format.")
        };
    }

    private async Task<string> ToCsvStringAsync(IEnumerable<ExpandoObject> records, InputParameter inputParameter)
    {
        using var writer = new StringWriter();
        using var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = inputParameter.Delimiter ?? ",",
            IgnoreBlankLines = inputParameter.IgnoreBlankLines ?? true,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = true,
            BadDataFound = null
        });

        // Write header
        var firstRecord = records.FirstOrDefault();
        if (firstRecord is not null)
        {
            var headerRow = ((IDictionary<string, object?>)firstRecord).Keys;
            foreach (var header in headerRow)
            {
                csvWriter.WriteField(header);
                _logger?.LogInfo($"Column: '{header}'");
            }
            await csvWriter.NextRecordAsync();

            // Write rows
            foreach (var record in records)
            {
                var values = (IDictionary<string, object?>)record;
                foreach (var value in values.Values)
                {
                    csvWriter.WriteField(value);
                    _logger?.LogInfo($"Value: '{value}'");
                }
                await csvWriter.NextRecordAsync();
            }
        }

        await csvWriter.FlushAsync();

        _logger?.LogInfo(writer.ToString());

        return writer.ToString();
    }
}