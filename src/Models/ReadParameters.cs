namespace FlowSynx.Plugins.Csv.Models;

internal class ReadParameters
{
    public string Path { get; set; } = string.Empty;
    public string? Delimiter { get; set; } = ",";
    public bool? HasHader { get; set; } = true;
}