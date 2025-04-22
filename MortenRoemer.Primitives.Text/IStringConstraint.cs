using MortenRoemer.Primitives.Text.Constraint;
using MortenRoemer.Primitives.Text.Exception;
using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

/// <summary>
/// Represents a specific constraint a string has to satisfy. For example being in all uppercase letters.<br/>
/// This interface is used in defining constraints to use in <see cref="ConstrainedString{TConstraint}"/> instances.<br/>
/// <br/>
/// <list type="bullet">
///     <listheader>
///         <term>This library defines already the following constrains:</term>
///     </listheader>
///     <item>
///         <term><see cref="AsciiIdentifierConstraint"/></term>
///     </item>
///     <item>
///         <term><see cref="JsonConstraint"/></term>
///     </item>
///     <item>
///         <term><see cref="XmlConstraint"/></term>
///     </item>
/// </list>
/// </summary>
[ImmutableMemoryAccess(Reason = "These constraints should not use any shared resources as they are frequently used " +
                                "over multiple threads and even synchronization might add considerable overhead")]
public interface IStringConstraint
{
    /// <summary>
    /// Verifies that the provided <c>text</c> satisfies the constraint defined by this class.
    /// The constraint may reject the provided <c>text</c> for any reason, but should provide precise reasons for any
    /// rejection. It should also provide a specific index where the constraint failed, if possible.
    /// </summary>
    /// <param name="text">
    /// The string to verify against this constraint. This may be <c>""</c> but will never be
    /// <c>null</c>.
    /// </param>
    /// <returns>
    /// an instance of the <see cref="ConstraintResult"/> struct. <see cref="ConstraintResult"/> provides either the
    /// static method <see cref="ConstraintResult.Accept()"/> to accept the <c>text</c> or the method
    /// <see cref="ConstraintResult.Deny(string, int?)"/> to reject the <c>text</c> provided.
    /// </returns>
    /// <remarks>
    /// Please note that any unhandled exception thrown during verification will stop the verification process
    /// and will result in an inconclusive result. If that happens in an instance of
    /// <see cref="ConstrainedString{TConstraint}"/> then this will lead to a
    /// <see cref="VerificationFailedException"/> which is different to a <see cref="ConstraintFailedException"/>.
    /// </remarks>
    public static abstract ConstraintResult Verify(string text);
}