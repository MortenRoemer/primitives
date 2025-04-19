namespace MortenRoemer.Primitives.Text.Constraint;

/// <summary>
/// A predefined <see cref="IStringConstraint"/> to be used in <see cref="ConstrainedString{TConstraint}"/>.
/// This constraint accepts any string that is not empty and only contains letters and digits found in the ASCII
/// code-page or underscores <c>_</c>
/// </summary>
/// <example>
/// The following examples are therefore valid under this constraint:
/// <code>
/// AsciiIdentifierConstraint.Verify("sd_helena"); // true
/// AsciiIdentifierConstraint.Verify("Aloy"); // true
/// AsciiIdentifierConstraint.Verify(""); // false, because its empty
/// AsciiIdentifierConstraint.Verify("Lorum Ipsum"); // false, because it contains whitespace
/// </code>
/// </example>
public abstract class AsciiIdentifierConstraint : IStringConstraint
{
    private const string Message = "ascii identifiers may not be empty and only contain ascii letters, digits or underscores";

    private AsciiIdentifierConstraint()
    {
        // this class my never be instantiated
    }
    
    /// <inheritdoc cref="IStringConstraint.Verify(string)"/>
    public static ConstraintResult Verify(string text)
    {
        if (text.Length == 0)
            return ConstraintResult.Deny(Message);
        
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
                return ConstraintResult.Deny(Message, index);
        }

        return ConstraintResult.Accept;
    }
}