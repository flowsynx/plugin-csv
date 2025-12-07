using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.Csv.Operations.Read;

public class ReadParameters
{
    [OperationParameterMetadata(Description = "The delimiter used in the CSV content.", IsRequired = false)]
    public string? Delimiter { get; set; } = ",";

    [OperationParameterMetadata(Description = "Indicates whether the CSV has a header row.", IsRequired = false)]
    public bool? HasHeader { get; set; } = true;

    [OperationParameterMetadata(Description = "Indicates whether to ignore blank lines in the CSV.", IsRequired = false)]
    public bool? IgnoreBlankLines { get; set; } = true;

    [OperationParameterMetadata(Description = "The CSV content to read data from.", IsRequired = true)]
    public object? Data { get; set; }
}