using FluentAssertions;
using MortenRoemer.Primitives.Text;
using MortenRoemer.Primitives.Text.Constraint;
using MortenRoemer.Primitives.Text.Utf8;

namespace Tests;

public sealed class Utf8EncodedTest
{
    [Fact]
    public void StringUtf8EncodeShouldWork()
    {
        const string stringValue = "abc";
        var expectedResult = "abc"u8.ToArray();

        var actualResult = stringValue.Utf8Encode();

        actualResult.EncodedValue.ToArray().Should().BeEquivalentTo(expectedResult);
    }
    
    [Fact]
    public void ConstrainedStringUtf8EncodeShouldWork()
    {
        var stringValue = ConstrainedString<AsciiIdentifierConstraint>.Create("abc");
        var expectedResult = "abc"u8.ToArray();

        var actualResult = stringValue.Utf8Encode();

        actualResult.EncodedValue.ToArray().Should().BeEquivalentTo(expectedResult);
    }
}