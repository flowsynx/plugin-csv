using CsvHelper;
using CsvHelper.Configuration;
using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Helpers;
using FlowSynx.Plugins.Csv.Models;
using FlowSynx.Plugins.Csv.Services;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;

namespace FlowSynx.Plugins.Csv.Operations.Filter;

internal class FilterOperation : IPluginOperation<FilterParameters, PluginContext>
{
    private readonly IGuidProvider _guidProvider = new GuidProvider();

    public string Name => "Filter";

    public string Description => "Filters CSV data based on specified conditions.";

    public async Task<PluginContext?> ExecuteAsync(FilterParameters parameters, CancellationToken cancellationToken)
    {
        if (parameters.Filters is null)
            throw new ArgumentException("Missing or invalid 'filters' argument.");

        var isJson = IsJson(parameters.Filters?.ToString());
        if (!isJson)
            throw new ArgumentException("Invalid filter structure.");

        // Use case-insensitive property name matching so JSON like {"logic":..., "filters":...} binds correctly
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rootGroup = JsonSerializer.Deserialize<FilterGroup>(parameters.Filters?.ToString()!, jsonOptions);

        if (rootGroup is null)
            throw new ArgumentException("Invalid filter structure.");

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

        var result = records.Where(row => EvaluateFilterGroup(row, rootGroup)).ToList();
        var csvString = await helper.ToCsvStringAsync(result, parameters.Delimiter, parameters.IgnoreBlankLines, parameters.HasHeader);



        var rows = result as IEnumerable<ExpandoObject>;
        if (rows == null)
            throw new ArgumentException("Missing 'records' parameter.");

        var structuredData = rows
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

    private static bool IsJson(string? input)
    {
        input = ((input == null) ? string.Empty : input.Trim());
        if (!input.StartsWith("{") || !input.EndsWith("}"))
        {
            if (input.StartsWith("["))
            {
                return input.EndsWith("]");
            }

            return false;
        }

        return true;
    }

    private static bool EvaluateFilterGroup(ExpandoObject row, FilterGroup group)
    {
        var results = group.Filters.Select(f =>
        {
            if (f.IsGroup)
            {
                return EvaluateFilterGroup(row, f.Group!);
            }
            else
            {
                return EvaluateFilter(row, f);
            }
        }).ToList();

        return group.Logic.ToLowerInvariant() switch
        {
            "or" => results.Any(r => r),
            _ => results.All(r => r), // default is AND
        };
    }

    private static bool EvaluateFilter(ExpandoObject row, FilterCondition filter)
    {
        var dict = (IDictionary<string, object?>)row;
        if (!dict.TryGetValue(filter.Column!, out var cellValue))
            return false;

        var cellString = cellValue?.ToString() ?? string.Empty;

        if (TryParseNumber(cellString, out var cellNumber) &&
            TryParseNumber(filter.Value, out var filterNumber))
        {
            return EvaluateNumberComparison(cellNumber, filterNumber, filter.Operator!);
        }
        if (TryParseDate(cellString, out var cellDate) &&
            TryParseDate(filter.Value, out var filterDate))
        {
            return EvaluateDateComparison(cellDate, filterDate, filter.Operator!);
        }

        return EvaluateStringComparison(cellString, filter.Value ?? string.Empty, filter.Operator!);
    }

    private static bool EvaluateStringComparison(string cell, string filterValue, string op) =>
        op switch
        {
            "equals" => string.Equals(cell, filterValue, StringComparison.OrdinalIgnoreCase),
            "notEquals" => !string.Equals(cell, filterValue, StringComparison.OrdinalIgnoreCase),
            "contains" => cell.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            "startsWith" => cell.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            "endsWith" => cell.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            _ => throw new NotSupportedException($"String operator '{op}' is not supported.")
        };

    private static bool EvaluateNumberComparison(double cell, double filter, string op) =>
        op switch
        {
            "equals" => cell == filter,
            "notEquals" => cell != filter,
            "greaterThan" => cell > filter,
            "lessThan" => cell < filter,
            "greaterThanOrEqual" => cell >= filter,
            "lessThanOrEqual" => cell <= filter,
            _ => throw new NotSupportedException($"Numeric operator '{op}' is not supported.")
        };

    private static bool EvaluateDateComparison(DateTime cell, DateTime filter, string op) =>
        op switch
        {
            "equals" => cell == filter,
            "notEquals" => cell != filter,
            "greaterThan" => cell > filter,
            "lessThan" => cell < filter,
            "greaterThanOrEqual" => cell >= filter,
            "lessThanOrEqual" => cell <= filter,
            _ => throw new NotSupportedException($"Date operator '{op}' is not supported.")
        };

    private static bool TryParseNumber(string? input, out double number) =>
        double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

    private static bool TryParseDate(string? input, out DateTime date) =>
        DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}