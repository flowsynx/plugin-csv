using FlowSynx.Plugins.Csv.Services;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Services;

public class DefaultReflectionGuardTests
{
    [Fact]
    public void IsCalledViaReflection_ReturnsFalseInNormalCall()
    {
        var guard = new DefaultReflectionGuard();
        var result = guard.IsCalledViaReflection();
        Assert.False(result);
    }
}
