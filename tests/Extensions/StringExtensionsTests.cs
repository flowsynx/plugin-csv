using FlowSynx.Plugins.Csv.Extensions;
using Xunit;

namespace FlowSynx.Pluign.Csv.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("YWJjZA==", true)] // "abcd"
    [InlineData("YWJ jZA==", false)]
    [InlineData("YWJjZA==\n", false)]
    public void IsBase64String_Works(string input, bool expected)
    {
        var result = FlowSynx.Plugins.Csv.Extensions.StringExtensions.IsBase64String(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToByteArray_ConvertsUtf8()
    {
        var bytes = FlowSynx.Plugins.Csv.Extensions.StringExtensions.ToByteArray("hello");
        Assert.Equal(new byte[]{104,101,108,108,111}, bytes);
    }

    [Fact]
    public void Base64ToByteArray_Converts()
    {
        var bytes = FlowSynx.Plugins.Csv.Extensions.StringExtensions.Base64ToByteArray("aGVsbG8="); // "hello"
        Assert.Equal(new byte[]{104,101,108,108,111}, bytes);
    }
}
