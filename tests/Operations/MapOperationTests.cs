using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Operations.Map;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Operations;

public class MapOperationTests
{
    [Fact]
    public async Task ExecuteAsync_ProjectsColumns()
    {
        var op = new MapOperation();
        var parms = new MapParameters
        {
            Data = "Name,Age,City\nA,1,X\nB,2,Y",
            Mappings = new[]{"Name","City"}
        };
        var ctx = await op.ExecuteAsync(parms, CancellationToken.None);
        Assert.NotNull(ctx);
        Assert.Equal("Csv", ctx!.Format);
        Assert.NotNull(ctx.StructuredData);
        Assert.Equal(2, ctx.StructuredData!.Count);
        Assert.True(ctx.StructuredData![0].ContainsKey("Name"));
        Assert.True(ctx.StructuredData![0].ContainsKey("Age"));
    }

    [Fact]
    public async Task ExecuteAsync_MissingMappings_Throws()
    {
        var op = new MapOperation();
        var parms = new MapParameters
        {
            Data = "Name,Age\nA,1\nB,2",
            Mappings = null
        };
        await Assert.ThrowsAsync<ArgumentException>(() => op.ExecuteAsync(parms, CancellationToken.None));
    }
}
