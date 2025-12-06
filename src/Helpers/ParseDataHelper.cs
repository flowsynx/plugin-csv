using CsvHelper;
using CsvHelper.Configuration;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Models;
using FlowSynx.Plugins.Csv.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Text;

namespace FlowSynx.Plugins.Csv.Helpers;

internal class ParseDataHelper
{
    private readonly IGuidProvider _guidProvider;

    public ParseDataHelper(IGuidProvider guidProvider)
    {
        _guidProvider = guidProvider ?? throw new ArgumentNullException(nameof(guidProvider));
    }

    public PluginContext ParseDataToContext(object? data)
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

    public string ReadDataFromPluginContext(PluginContext pluginContext, string? delimiter, bool? hasHeader)
    {
        if (pluginContext.Content is not null)
            return pluginContext.Content;
        else if (pluginContext.StructuredData is not null)
            return StructuredDataToCsv(pluginContext.StructuredData, delimiter, hasHeader);
        else
            throw new InvalidDataException(string.Format(Resources.TheEnteredDataIsInvalid, pluginContext.Id));
    }

    private string StructuredDataToCsv(List<Dictionary<string, object>>? data, string? delimiter = ",", bool? hasHeader = true)
    {
        if (data == null || data.Count == 0)
            return string.Empty;

        using var writer = new StringWriter();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter ?? ",",
            HasHeaderRecord = hasHeader ?? true,
            TrimOptions = TrimOptions.Trim,
            DetectColumnCountChanges = true,
            BadDataFound = null
        };

        using var csv = new CsvWriter(writer, config);

        // Get all unique headers
        var headers = data.SelectMany(d => d.Keys).Distinct().ToList();

        // Write headers
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        csv.NextRecord();

        // Write rows
        foreach (var row in data)
        {
            foreach (var header in headers)
            {
                row.TryGetValue(header, out var value);
                csv.WriteField(value);
            }
            csv.NextRecord();
        }

        return writer.ToString();
    }

    public async Task<string> ToCsvStringAsync(
        IEnumerable<ExpandoObject> records, 
        string? delimiter,
        bool? ignoreBlankLines,
        bool? hasHeader)
    {
        using var writer = new StringWriter();
        using var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter ?? ",",
            IgnoreBlankLines = ignoreBlankLines ?? true,
            HasHeaderRecord = hasHeader ?? true,
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
            }
            await csvWriter.NextRecordAsync();

            // Write rows
            foreach (var record in records)
            {
                var values = (IDictionary<string, object?>)record;
                foreach (var value in values.Values)
                {
                    csvWriter.WriteField(value);
                }
                await csvWriter.NextRecordAsync();
            }
        }

        await csvWriter.FlushAsync();
        return writer.ToString();
    }
}
