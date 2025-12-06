using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Csv.Parameters;

public class MapParameters
{
    [OperationParameterMetadata(Description = "The delimiter used in the CSV content.", IsRequired = false)]
    public string? Delimiter { get; set; } = ",";

    [OperationParameterMetadata(Description = "Indicates whether the CSV content has a header row.", IsRequired = false)]
    public bool? HasHeader { get; set; } = true;

    [OperationParameterMetadata(Description = "Indicates whether to ignore blank lines in the CSV content.", IsRequired = false)]
    public bool? IgnoreBlankLines { get; set; } = true;

    [OperationParameterMetadata(Description = "The CSV data to be mapped.", IsRequired = true)]
    public object? Data { get; set; }

    [OperationParameterMetadata(Description = "The list of column names to map.", IsRequired = true)]
    public IEnumerable<string>? Mappings { get; set; }
}