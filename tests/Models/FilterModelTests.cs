using FlowSynx.Plugins.Csv.Models;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Models;

public class FilterModelTests
{
    [Fact]
    public void FilterCondition_GroupDetection()
    {
        var cond = new FilterCondition
        {
            Logic = "or",
            Filters = new List<FilterCondition>{ new(){ Column = "A", Operator = "equals", Value = "1" } }
        };
        Assert.True(cond.IsGroup);
        Assert.NotNull(cond.Group);
        Assert.Equal("or", cond.Group!.Logic);
        Assert.Single(cond.Group!.Filters);
    }

    [Fact]
    public void FilterGroup_Defaults()
    {
        var group = new FilterGroup();
        Assert.Equal("and", group.Logic);
        Assert.Empty(group.Filters);
    }
}
