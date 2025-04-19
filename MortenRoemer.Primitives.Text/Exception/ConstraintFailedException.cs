namespace MortenRoemer.Primitives.Text.Exception;

/// <summary>
/// An Exception that should be thrown, if a <see cref="IStringConstraint"/> was not satisfied and the process is
/// unable to continue.
/// </summary>
public sealed class ConstraintFailedException : System.Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="ConstraintFailedException"/>
    /// </summary>
    /// <param name="message">The message that hints at the reason the constraint was not satisfied</param>
    /// <param name="deniedString">The string that was denied by the constraint</param>
    /// <param name="typeName">The Constraint type name that was not satisfied</param>
    /// <param name="index">Optionally the index in the string where the constraint was not satisfied</param>
    public ConstraintFailedException(string message, string deniedString, string typeName, int? index)
        : base(FormatMessage(message, deniedString, typeName, index))
    {
        DeniedString = deniedString;
        TypeName = typeName;
        Index = index;
    }
    
    /// <summary>
    /// The specific string that was denied by the constraint
    /// </summary>
    public readonly string DeniedString;
    
    /// <summary>
    /// The name of the <see cref="IStringConstraint"/> implementation type that denied this string
    /// </summary>
    public readonly string TypeName;

    /// <summary>
    /// If present, the index in the denied string where the constraint failed
    /// </summary>
    public readonly int? Index;
    
    private static string FormatMessage(string message, string deniedString, string typeName, int? index)
    {
        return index.HasValue 
            ? $"text \"{deniedString}\" did not satisfy constraint with name {typeName} and failed at index {index} with message: {message}" 
            : $"text \"{deniedString}\" did not satisfy constraint with name {typeName} and failed with message: {message}";
    }
}