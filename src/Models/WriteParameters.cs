namespace FlowSynx.Plugins.Csv.Models;

internal class WriteParameters
{
    public string Path { get; set; } = string.Empty;
    public string? Delimiter { get; set; } = ",";
    public bool? WriteHader { get; set; } = true;
    public object? Data { get; set; }
    public bool Overwrite { get; set; } = false;
}