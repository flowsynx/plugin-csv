using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Operations.Filter;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Operations;

public class FilterOperationTests
{
    [Fact]
    public async Task ExecuteAsync_FiltersRows()
    {
        var op = new FilterOperation();
        var filtersJson = "{\"logic\":\"and\",\"filters\":[{\"column\":\"Age\",\"operator\":\"greaterThan\",\"value\":\"1\"}]}";
        var parms = new FilterParameters
        {
            Data = "Name,Age\nA,1\nB,2",
            Delimiter = ",",
            HasHeader = true,
            IgnoreBlankLines = true,
            Filters = filtersJson
        };
        var ctx = await op.ExecuteAsync(parms, CancellationToken.None);
        Assert.NotNull(ctx);
        Assert.Equal("Csv", ctx!.Format);
        Assert.NotNull(ctx.StructuredData);
        Assert.Single(ctx.StructuredData!);
        Assert.Equal("B", ctx.StructuredData![0]["Name"]);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_Throws()
    {
        var op = new FilterOperation();
        var parms = new FilterParameters
        {
            Data = "Name,Age\nA,1\nB,2",
            Filters = "not json"
        };
        await Assert.ThrowsAsync<ArgumentException>(() => op.ExecuteAsync(parms, CancellationToken.None));
    }
}
