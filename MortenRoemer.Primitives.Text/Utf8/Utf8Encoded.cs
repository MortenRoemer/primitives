using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text.Utf8;

/// <summary>
/// Holds a pre-encoded UTF-8 representation of the value in a struct.
/// These can be created with the <see cref="Utf8Extension.Utf8Encode(string)"/> method for example.
/// </summary>
/// <typeparam name="TValue">The type of value to encode into UTF-8</typeparam>
[ImmutableMemoryAccess(Reason = "This type should only be generated once per value and then be reused. " +
                                "This is only safely possible with immutable structs")]
public readonly struct Utf8Encoded<TValue>
    where TValue : notnull
{
    /// <summary>
    /// Generates a new instance with the specified value and its encoded representation.
    /// </summary>
    /// <param name="value">The value that was encoded</param>
    /// <param name="encodedValue">The UTF-8 representation in bytes</param>
    public Utf8Encoded(TValue value, ReadOnlyMemory<byte> encodedValue)
    {
        Value = value;
        _encodedValue = encodedValue;
    }
    
    /// <summary>
    /// The value that is also encoded in UTF-8
    /// </summary>
    [SkipMemorySafetyCheck(Because = "Thread-Safety of generic types can not be verified")]
    public readonly TValue Value;
    
    private readonly ReadOnlyMemory<byte> _encodedValue;
    
    /// <summary>
    /// The value encoded as a stream of bytes encoded in UTF-8
    /// </summary>
    public ReadOnlySpan<byte> EncodedValue => _encodedValue.Span;

    /// <summary>
    /// Converts the value to its string representation
    /// </summary>
    /// <returns>a string representing the inner value</returns>
    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}