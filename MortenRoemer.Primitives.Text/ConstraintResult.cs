using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

/// <summary>
/// Represents a result of a constraint verification handled by instances of <see cref="IStringConstraint"/>.
/// An instance contains at least the property <see cref="Acceptable"/> to indicate if the string fulfills the
/// constraint. If not, then it also contains a <see cref="Message"/> and, optionally, an <see cref="Index"/>
/// where the constraint failed.
/// </summary>
/// <example>
/// instances of ConstraintResult may be created either through the static <see cref="Accept"/> method or
/// the static <see cref="Deny"/> method:
/// <code>
/// const string testString = "hey mr nice guy@someemailprovider.com";
/// var positiveResult = ConstraintResult.Accept;
/// var negativeResult = ConstraintResult.Deny("e-mail-addresses may not contain whitespace", 3);
/// </code>
/// </example>
[ImmutableMemoryAccess(Reason = "This type should only be generated once per verification and then be reused. " +
                                "This is only safely possible with immutable structs")]
public readonly struct ConstraintResult
{
    private ConstraintResult(bool accepted, string? message, int? index)
    {
        Acceptable = accepted;
        Message = message;
        Index = index;
    }
    
    /// <summary>
    /// Indicates a positive result after constraint verification.
    /// </summary>
    public static ConstraintResult Accept
        => new(true, null, null);

    /// <summary>
    /// Indicates a negative result after constraint verification.
    /// </summary>
    /// <param name="message">The message describing the reason for the result</param>
    /// <param name="index">optionally, the index of the character that led to the result</param>
    /// <returns>An instance of the result</returns>
    public static ConstraintResult Deny(string message, int? index = null)
        => new(false, message, index);
    
    /// <summary>
    /// Indicates, if the value was acceptable, given the constraint
    /// </summary>
    public readonly bool Acceptable;

    /// <summary>
    /// Optionally, contains a message describing the reason for the result
    /// </summary>
    public readonly string? Message;
    
    /// <summary>
    /// Optionally, contains the zero-based index of the character that led to the result
    /// </summary>
    public readonly int? Index;
}