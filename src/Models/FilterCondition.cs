namespace FlowSynx.Plugins.Csv.Models;

internal record FilterCondition
{
    public string? Column { get; init; }
    public string? Operator { get; init; }
    public string? Value { get; init; }
    public string? Logic { get; init; }
    public List<FilterCondition>? Filters { get; init; }
    public bool IsGroup => Filters is { Count: > 0 };
    public FilterGroup? Group => IsGroup
        ? new FilterGroup { Logic = Logic ?? "and", Filters = Filters! }
        : null;
}