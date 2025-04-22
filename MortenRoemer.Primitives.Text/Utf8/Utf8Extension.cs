using System.Text;
using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text.Utf8;

/// <summary>
/// Contains extension methods to generate UTF-8 encoded byte arrays from strings.
/// For example <see cref="Utf8Encode(string)"/>.
/// </summary>
[SynchronizedMemoryAccess(Reason = "extension methods are also used in multi-threading scenarios. " +
                                   "All mutable values should therefore be synchronized.")]
public static class Utf8Extension
{
    [SkipMemorySafetyCheck(Because = "instances of UTF8Encoding are thread-safe according to documentation")]
    private static readonly UTF8Encoding Utf8Encoding = new(
        encoderShouldEmitUTF8Identifier: false, 
        throwOnInvalidBytes: true
    );
    
    /// <summary>
    /// Encodes this string as a UTF-8 encoded array of bytes and returns an <see cref="Utf8Encoded{TValue}"/>.
    /// </summary>
    /// <param name="text">The text to encode</param>
    /// <returns>a struct that contains the UTF-8 encoded value</returns>
    public static Utf8Encoded<string> Utf8Encode(this string text)
    {
        var encodedString = Encoding.UTF8.GetBytes(text);
        return new Utf8Encoded<string>(text, encodedString);
    }
    
    /// <summary>
    /// Encodes this string as a UTF-8 encoded array of bytes and returns an <see cref="Utf8Encoded{TValue}"/>.
    /// </summary>
    /// <param name="text">The text to encode</param>
    /// <returns>a struct that contains the UTF-8 encoded value</returns>
    public static Utf8Encoded<ConstrainedString<TConstraint>> Utf8Encode<TConstraint>(
        this ConstrainedString<TConstraint> text
    ) where TConstraint : IStringConstraint
    {
        var encodedString = Encoding.UTF8.GetBytes(text);
        return new Utf8Encoded<ConstrainedString<TConstraint>>(text, encodedString);
    }

    /// <summary>
    /// Determines if the specified span of bytes only contains valid UTF-8 encoded characters
    /// without a Byte-Order-Mark.
    /// </summary>
    /// <param name="bytes">The span of bytes to check</param>
    /// <returns>true, if the byte span only contains valid UTF-8, otherwise false</returns>
    public static bool IsValidUtf8(this Span<byte> bytes)
        => IsValidUtf8((ReadOnlySpan<byte>)bytes);
    
    /// <summary>
    /// Determines if the specified span of bytes only contains valid UTF-8 encoded characters
    /// without a Byte-Order-Mark.
    /// </summary>
    /// <param name="bytes">The span of bytes to check</param>
    /// <returns>true, if the byte span only contains valid UTF-8, otherwise false</returns>
    public static bool IsValidUtf8(this ReadOnlySpan<byte> bytes)
    {
        try
        {
            Utf8Encoding.GetCharCount(bytes);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}