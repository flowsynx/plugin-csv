using FlowSynx.Plugins.Csv.Models;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;

namespace FlowSynx.Plugins.Csv.Services;

internal class FilterOperationHandler : ICsvOperationHandler
{
    public IEnumerable<ExpandoObject> Handle(IEnumerable<ExpandoObject> rows, InputParameter parameter)
    {
        if (parameter.Filters is null)
            throw new ArgumentException("Missing or invalid 'filters' argument.");

        var isJson = IsJson(parameter.Filters.ToString());
        if (!isJson)
            throw new ArgumentException("Invalid filter structure.");

        var rootGroup = JsonSerializer.Deserialize<FilterGroup>(parameter.Filters.ToString()!);

        if (rootGroup is null)
            throw new ArgumentException("Invalid filter structure.");

        return rows.Where(row => EvaluateFilterGroup(row, rootGroup)).ToList();
    }

    private bool IsJson(string? input)
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

    private bool EvaluateFilterGroup(ExpandoObject row, FilterGroup group)
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

    private bool EvaluateFilter(ExpandoObject row, FilterCondition filter)
    {
        var dict = (IDictionary<string, object>)row;
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

        return EvaluateStringComparison(cellString, filter.Value, filter.Operator!);
    }

    private bool EvaluateStringComparison(string cell, string filterValue, string op) =>
        op switch
        {
            "equals" => string.Equals(cell, filterValue, StringComparison.OrdinalIgnoreCase),
            "notEquals" => !string.Equals(cell, filterValue, StringComparison.OrdinalIgnoreCase),
            "contains" => cell.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            "startsWith" => cell.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            "endsWith" => cell.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            _ => throw new NotSupportedException($"String operator '{op}' is not supported.")
        };

    private bool EvaluateNumberComparison(double cell, double filter, string op) =>
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

    private bool EvaluateDateComparison(DateTime cell, DateTime filter, string op) =>
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

    private bool TryParseNumber(string input, out double number) =>
        double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

    private bool TryParseDate(string input, out DateTime date) =>
        DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}