using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Csv.Parameters;

public class FilterParameters
{
    [OperationParameterMetadata(Description = "The delimiter used in the CSV content.", IsRequired = false)]
    public string? Delimiter { get; set; } = ",";

    [OperationParameterMetadata(Description = "Indicates whether the CSV content has a header row.", IsRequired = false)]
    public bool? HasHeader { get; set; } = true;

    [OperationParameterMetadata(Description = "Indicates whether to ignore blank lines in the CSV content.", IsRequired = false)]
    public bool? IgnoreBlankLines { get; set; } = true;

    [OperationParameterMetadata(Description = "The CSV data to be filtered.", IsRequired = true)]
    public object? Data { get; set; }

    [OperationParameterMetadata(Description = "Filter conditions in JSON format.", IsRequired = true)]
    public object? Filters { get; set; }
}