﻿using System.Text.Json;

namespace MortenRoemer.Primitives.Text.Constraint;

/// <summary>
/// A predefined <see cref="IStringConstraint"/> to be used in <see cref="ConstrainedString{TConstraint}"/>.
/// This constraint accepts only valid JSON without comments
/// </summary>
/// <example>
/// The following examples are therefore valid under this constraint:
/// <code>
/// JsonConstraint.Verify("[1,2,3]");
/// JsonConstraint.Verify("{}");
/// </code>
/// </example>
public abstract class JsonConstraint : IStringConstraint
{
    private const string Message = "JSON files must conform to the specification of the JSON format without comments";

    private JsonConstraint()
    {
        // this class my never be instantiated
    }
    
    /// <inheritdoc cref="IStringConstraint.Verify(string)"/>
    public static ConstraintResult Verify(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return ConstraintResult.Accept;
        }
        catch (JsonException exception)
        {
            return ConstraintResult.Deny(exception.Message);
        }
    }
}