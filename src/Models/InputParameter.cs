namespace FlowSynx.Plugins.Csv.Models;

internal class InputParameter
{
    public string Operation { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Delimiter { get; set; } = ",";
    public bool? IgnoreBlankLines { get; set; } = true;
    public bool? HasHeader { get; set; } = true;
    public IEnumerable<string>? Mappings { get; set; }
    public object? Filters { get; set; }
}