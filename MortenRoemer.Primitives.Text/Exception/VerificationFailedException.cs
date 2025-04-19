namespace MortenRoemer.Primitives.Text.Exception;

/// <summary>
/// An exception that is thrown by <see cref="ConstrainedString{TConstraint}"/> if any exception occurs
/// during the constraint verification process itself. It should always indicate some kind of developer error
/// and should not occur in production.
/// </summary>
public sealed class VerificationFailedException : System.Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="VerificationFailedException"/>
    /// </summary>
    /// <param name="providedString">The string that led to the exception during verification</param>
    /// <param name="inner">The exception that occurred during verification</param>
    public VerificationFailedException(string providedString, System.Exception inner)
        : base($"Constraint verification for \"{providedString}\" failed with exception: {inner.Message}", inner)
    {
        ProvidedString = providedString;
    }
    
    /// <summary>
    /// The string that led to the exception during verification
    /// </summary>
    public readonly string ProvidedString;
}