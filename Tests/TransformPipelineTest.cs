using System.Globalization;
using FluentAssertions;
using MortenRoemer.Primitives.Text;
using MortenRoemer.Primitives.Text.Constraint;

namespace Tests;

public sealed class TransformPipelineTest
{
    [Fact]
    public void TransformPipelineShouldWorkForStrings()
    {
        const string input = "I'll be back";
        
        var output = input.TransformPipeline(buffer => buffer
            .Remove(1, 1)
            .Replace(' ', '_')
            .ToUpper(CultureInfo.InvariantCulture)
        );
        
        output.Should().Be("ILL_BE_BACK");
    }
    
    [Fact]
    public void TransformPipelineShouldWorkForConstrainedStrings()
    {
        var input = ConstrainedString<AsciiIdentifierConstraint>.Create("I_ll_be_back");
        
        var output = input.TransformPipeline(buffer => buffer
            .Remove(1, 1)
            .ToUpper(CultureInfo.InvariantCulture)
        );
        
        output.ToString().Should().Be("ILL_BE_BACK");
    }
}