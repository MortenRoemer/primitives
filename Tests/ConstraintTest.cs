using FluentAssertions;
using MortenRoemer.Primitives.Text.Constraint;

namespace Tests;

public sealed class ConstraintTest 
{
    [Theory]
    [InlineData("", false)]
    [InlineData("_", true)]
    [InlineData("My name is bond.", false)]
    [InlineData("My_name_is_bond_", true)]
    public void AsciiIdentifierConstraintShouldVerifyInputCorrectly(string input, bool expectedValue)
    {
        AsciiIdentifierConstraint.Verify(input).Acceptable.Should().Be(expectedValue);
    }

    
    [Theory]
    [InlineData("", false)]
    [InlineData("{}", true)]
    [InlineData("{\"value\": 52, \"message\": \"No, I am your father, Luke!\"}", true)]
    public void JsonConstraintShouldVerifyInputCorrectly(string input, bool expectedValue)
    {
        JsonConstraint.Verify(input).Acceptable.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("<someNode></someNode>", true)]
    public void XmlConstraintShouldVerifyInputCorrectly(string input, bool expectedValue)
    {
        XmlConstraint.Verify(input).Acceptable.Should().Be(expectedValue);
    }
}