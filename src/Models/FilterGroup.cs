namespace FlowSynx.Plugins.Csv.Models;

internal record FilterGroup
{
    public string Logic { get; init; } = "and"; // "and" or "or"
    public List<FilterCondition> Filters { get; init; } = new();
}