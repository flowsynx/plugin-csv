using FlowSynx.PluginCore;
using FlowSynx.Plugins.Csv.Helpers;
using FlowSynx.Plugins.Csv.Services;
using System.Dynamic;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Helpers;

public class ParseDataHelperTests
{
    private class FixedGuidProvider : IGuidProvider
    {
        private readonly Guid _guid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Guid NewGuid() => _guid;
    }

    [Fact]
    public void ParseDataToContext_Null_Throws()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        Assert.Throws<ArgumentNullException>(() => helper.ParseDataToContext(null));
    }

    [Fact]
    public void ParseDataToContext_String_Wrapped()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        var ctx = helper.ParseDataToContext("a,b\n1,2");
        Assert.Equal("00000000-0000-0000-0000-000000000001", ctx.Id);
        Assert.Equal("a,b\n1,2", ctx.Content);
    }

    [Fact]
    public void ParseDataToContext_PluginContext_PassedThrough()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        var input = new PluginContext("id","Data"){ Content = "c1,c2\n3,4" };
        var ctx = helper.ParseDataToContext(input);
        Assert.Same(input, ctx);
    }

    [Fact]
    public void ParseDataToContext_ListOfContext_NotSupported()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        Assert.Throws<NotSupportedException>(() => helper.ParseDataToContext(new List<PluginContext>()));
    }

    [Fact]
    public void ReadDataFromPluginContext_ReturnsContent()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        var ctx = new PluginContext("id","Data"){ Content = "c1,c2\n3,4" };
        var csv = helper.ReadDataFromPluginContext(ctx, ",", true);
        Assert.Equal("c1,c2\n3,4", csv);
    }

    [Fact]
    public void ReadDataFromPluginContext_FromStructuredData()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        var rows = new List<Dictionary<string, object>>
        {
            new() { {"c1", 3}, {"c2", 4} },
            new() { {"c1", 5}, {"c3", 6} }
        };
        var ctx = new PluginContext("id","Data"){ StructuredData = rows };
        var csv = helper.ReadDataFromPluginContext(ctx, ",", true);
        Assert.Contains("c1,c2,c3", csv);
        Assert.Contains("3,4,", csv);
        Assert.Contains("5,,6", csv);
    }

    [Fact]
    public async Task ToCsvStringAsync_WritesHeaderAndRows()
    {
        var helper = new ParseDataHelper(new FixedGuidProvider());
        var rows = new List<System.Dynamic.ExpandoObject>();
        dynamic r1 = new System.Dynamic.ExpandoObject(); r1.Name = "A"; r1.Age = 1; rows.Add(r1);
        dynamic r2 = new System.Dynamic.ExpandoObject(); r2.Name = "B"; r2.Age = 2; rows.Add(r2);
        var csv = await helper.ToCsvStringAsync(rows, ",", true, true);
        Assert.Contains("Name,Age", csv);
        Assert.Contains("A,1", csv);
        Assert.Contains("B,2", csv);
    }
}
