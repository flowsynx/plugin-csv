using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv;
using FlowSynx.Plugins.Csv.Services;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests;

public class CsvPluginTests
{
    private class FakeGuidProvider : IGuidProvider
    {
        public Guid NewGuid() => Guid.Parse("00000000-0000-0000-0000-000000000123");
    }

    private class FakeReflectionGuard : IReflectionGuard
    {
        public bool IsCalledViaReflection() => false;
    }

    private class DummyLogger : IPluginLogger
    {
        public void Log(PluginLoggerLevel level, string message) { }
    }

    [Fact]
    public void Metadata_HasExpectedValues()
    {
        var plugin = new CsvPlugin();
        var md = plugin.Metadata;
        Assert.Equal("Csv", md.Name);
        Assert.Equal("FlowSynx", md.CompanyName);
        Assert.Equal("flowsynx.png", md.Icon);
        Assert.Equal("README.md", md.ReadMe);
        Assert.Equal(1, md.Version.Major);
    }

    [Fact]
    public async Task InitializeAsync_SetsSpecifications()
    {
        var plugin = new CsvPlugin(new FakeGuidProvider(), new FakeReflectionGuard());
        await plugin.InitializeAsync(new DummyLogger(), new Dictionary<string, object?>());
        Assert.NotNull(plugin.Specifications);
    }

    [Fact]
    public async Task ExecuteAsync_BeforeInit_Throws()
    {
        var plugin = new CsvPlugin(new FakeGuidProvider(), new FakeReflectionGuard());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            plugin.ExecuteAsync("read", new PluginParameters(), CancellationToken.None));
        Assert.Contains("not initialized", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedOperation_Throws()
    {
        var plugin = new CsvPlugin(new FakeGuidProvider(), new FakeReflectionGuard());
        await plugin.InitializeAsync(new DummyLogger(), null);
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            plugin.ExecuteAsync("unknown", new PluginParameters(), CancellationToken.None));
        Assert.Contains("not supported", ex.Message);
    }
}
