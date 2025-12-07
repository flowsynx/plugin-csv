using CsvHelper;
using CsvHelper.Configuration;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Helpers;
using FlowSynx.Plugins.Csv.Services;
using System.Dynamic;
using System.Globalization;

namespace FlowSynx.Plugins.Csv.Operations.Read;

internal class ReadOperation : IPluginOperation<ReadParameters, PluginContext>
{
    private readonly IGuidProvider _guidProvider = new GuidProvider();

    public string Name => "Read";

    public string Description => "Reads data from a CSV content.";

    public async Task<PluginContext?> ExecuteAsync(ReadParameters parameters, CancellationToken cancellationToken)
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