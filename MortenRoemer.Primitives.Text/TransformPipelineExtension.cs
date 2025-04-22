using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

/// <summary>
/// Contains extension methods to define pipelines, that means sequential actions against a string, that are
/// executed against a buffer to prevent frequent allocations.
/// For example <see cref="TransformPipeline(string, Action{CharBuffer})"/>
/// </summary>
[SynchronizedMemoryAccess(Reason = "extension methods are also used in multi-threading scenarios. " +
                                   "All mutable values should therefore be synchronized.")]
public static class TransformPipelineExtension
{
    /// <summary>
    /// Executes the given action against this string value and returns the result as a <see cref="string"/>.
    /// This action is executed against an instance of <see cref="CharBuffer"/> to prevent frequent allocations.
    /// </summary>
    /// <param name="value">The string value to transform</param>
    /// <param name="action">The action to be executed against the string</param>
    /// <returns>A new string that contains the resulting value</returns>
    public static string TransformPipeline(this string value, Action<CharBuffer> action)
    {
        using var buffer = new CharBuffer(value);
        action(buffer);
        return buffer.ToString();
    }

    /// <summary>
    /// Executes the given action against this string value and returns the result as a <see cref="ConstrainedString{T}"/>.
    /// This action is executed against an instance of <see cref="CharBuffer"/> to prevent frequent allocations.
    /// </summary>
    /// <param name="value">The string value to transform</param>
    /// <param name="action">The action to be executed against the string</param>
    /// <typeparam name="TConstraint">The constraint to verify after the action</typeparam>
    /// <returns>A new <see cref="ConstrainedString{T}"/> that contains the resulting value</returns>
    public static ConstrainedString<TConstraint> TransformPipeline<TConstraint>(
        this ConstrainedString<TConstraint> value,
        Action<CharBuffer> action
    ) where TConstraint : IStringConstraint
    {
        using var buffer = new CharBuffer(value.ToString());
        action(buffer);
        return ConstrainedString<TConstraint>.Create(buffer.ToString());
    }
}