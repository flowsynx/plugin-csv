using CsvHelper;
using CsvHelper.Configuration;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Helpers;
using FlowSynx.Plugins.Csv.Parameters;
using FlowSynx.Plugins.Csv.Services;
using System.Dynamic;
using System.Globalization;

namespace FlowSynx.Plugins.Csv.Operations;

internal class MapOperation : IPluginOperation<MapParameters, PluginContext>
{
    private readonly IGuidProvider _guidProvider = new GuidProvider();

    public string Name => "Map";

    public string Description => "Maps CSV data to a different structure.";

    public async Task<PluginContext?> ExecuteAsync(MapParameters parameters, CancellationToken cancellationToken)
    {
        var helper = new ParseDataHelper(_guidProvider);

        var context = helper.ParseDataToContext(parameters.Data);
        var csv = helper.ReadDataFromPluginContext(context, parameters.Delimiter, parameters.HasHeader);

        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = parameters.Delimiter ?? ",",
            IgnoreBlankLines = parameters.IgnoreBlankLines ?? true,
            HasHeaderRecord = parameters.HasHeader ?? true,
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

        var csvString = await helper.ToCsvStringAsync(records, parameters.Delimiter, parameters.IgnoreBlankLines, parameters.HasHeader);

        if (parameters.Mappings is not IEnumerable<string?> columns)
            throw new ArgumentException("Missing 'mapping' argument.");

        var selectedColumns = columns
            .OfType<string>()
            .ToList();

        var rows = records as IEnumerable<ExpandoObject>;
        if (rows == null)
            throw new ArgumentException("Missing 'records' parameter.");

        var projectedRows = rows.Select(row =>
        {
            var dict = (IDictionary<string, object?>)row;
            IDictionary<string, object?> projected = new ExpandoObject();
            foreach (var col in selectedColumns)
            {
                if (dict.TryGetValue(col, out var value))
                    projected[col] = value;
                else
                    projected[col] = null;
            }
            return (ExpandoObject)projected;
        });

        var structuredData = records
            .Select(expando => ((IDictionary<string, object?>)expando)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value as object))
            .ToList();

        string filename = $"{_guidProvider.NewGuid()}.csv";
        return new PluginContext(filename, "Data")
        {
            Format = "Csv",
            Content = csvString,
            StructuredData = structuredData
        };
    }
}