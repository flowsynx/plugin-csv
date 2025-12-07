using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Operations.Read;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Operations;

public class ReadOperationTests
{
    [Fact]
    public async Task ExecuteAsync_ReadsCsv()
    {
        var op = new ReadOperation();
        var parms = new ReadParameters
        {
            Data = "Name,Age\nA,1\nB,2",
            Delimiter = ",",
            HasHeader = true,
            IgnoreBlankLines = true
        };
        var ctx = await op.ExecuteAsync(parms, CancellationToken.None);
        Assert.NotNull(ctx);
        Assert.Equal("Csv", ctx!.Format);
        Assert.Contains("Name,Age", ctx.Content);
        Assert.NotNull(ctx.StructuredData);
        Assert.Equal(2, ctx.StructuredData!.Count);
    }
}
