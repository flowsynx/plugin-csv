using FlowSynx.Plugins.Csv.Models;
using System.Dynamic;

namespace FlowSynx.Plugins.Csv.Services;

internal class MapOperationHandler : ICsvOperationHandler
{
    public IEnumerable<ExpandoObject> Handle(IEnumerable<ExpandoObject> rows, InputParameter parameter)
    {
        if (parameter.Mappings is not IEnumerable<string?> columns)
            throw new ArgumentException("Missing 'mapping' argument.");

        var selectedColumns = columns
            .OfType<string>()
            .ToList();

        var projectedRows = rows.Select(row =>
        {
            var dict = (IDictionary<string, object>)row;
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

        return projectedRows;
    }
}