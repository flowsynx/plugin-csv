using FlowSynx.Plugins.Csv.Services;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Services;

public class GuidProviderTests
{
    [Fact]
    public void NewGuid_ReturnsUnique()
    {
        var provider = new GuidProvider();
        var g1 = provider.NewGuid();
        var g2 = provider.NewGuid();
        Assert.NotEqual(g1, g2);
        Assert.NotEqual(Guid.Empty, g1);
    }
}
