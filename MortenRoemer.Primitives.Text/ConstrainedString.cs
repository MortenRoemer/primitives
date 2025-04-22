using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MortenRoemer.Primitives.Text.Exception;
using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

/// <summary>
/// Represents a string instance that always holds to a specific constraint.
/// For example that it represents a valid e-mail-address.
/// </summary>
/// <typeparam name="TConstraint">The constraint to check against. See <see cref="IStringConstraint"/></typeparam>
/// <example>
/// Instances can be created with the static <see cref="Create"/> method:
/// <code>
/// var identifier = ConstrainedString&lt;AsciiIdentifierConstraint&gt;.Create("core_name");
/// </code>
/// </example>
[ImmutableMemoryAccess(Reason = "This type is immutable by design to be as close to a standard library string as " +
                                "possible")]
public readonly struct ConstrainedString<TConstraint> : IEquatable<ConstrainedString<TConstraint>>
    where TConstraint : IStringConstraint
{
    private ConstrainedString(string innerValue)
    {
        _innerValue = innerValue;
    }
    
    private readonly string _innerValue;

    /// <summary>
    /// Creates a new instance of a constrained string, verifying the given string.
    /// </summary>
    /// <param name="value">The string to verify</param>
    /// <returns>The given string value as a constrained string</returns>
    /// <exception cref="ConstraintFailedException">is thrown if the string does not fulfill the constraint</exception>
    /// <exception cref="VerificationFailedException">
    /// is thrown if the verification itself, most likely because an exception occurred during verification
    /// </exception>
    public static ConstrainedString<TConstraint> Create(string value)
    {
        try
        {
            var result = TConstraint.Verify(value);

            if (result.Acceptable)
                return new ConstrainedString<TConstraint>(value);

            throw new ConstraintFailedException(result.Message!, value, nameof(TConstraint), result.Index);
        }
        catch (ConstraintFailedException)
        {
            throw;
        }
        catch (System.Exception exception)
        {
            throw new VerificationFailedException(value, exception);
        }
    }
    
    /// <summary>
    /// Provides the content of this string as a span of characters
    /// </summary>
    public ReadOnlySpan<char> AsSpan => _innerValue.AsSpan();
    
    /// <inheritdoc cref="string.Length"/>
    public int Length => _innerValue.Length;
    
    /// <inheritdoc cref="string.this"/>
    public char this[int index] => _innerValue[index];
    
    /// <inheritdoc cref="string.Contains(char)"/>
    public bool Contains(char value)
        => AsSpan.Contains(value);
    
    /// <inheritdoc cref="string.Contains(string)"/>
    public bool Contains(ReadOnlySpan<char> value)
        => AsSpan.Contains(value, StringComparison.Ordinal);
    
    /// <inheritdoc cref="string.CopyTo(Span{char})"/>
    public void CopyTo(Span<char> destination)
        => AsSpan.CopyTo(destination);
    
    /// <inheritdoc cref="string.EndsWith(char)"/>
    public bool EndsWith(char value)
        => Length > 0 && AsSpan[^1] == value;

    /// <inheritdoc cref="string.EndsWith(string)"/>
    public bool EndsWith(ReadOnlySpan<char> value)
        => AsSpan.EndsWith(value);

    /// <summary>
    /// Compares this string instance to another determining if they are equal.
    /// </summary>
    /// <param name="other">The other string to compare against</param>
    /// <returns>true id the strings are equal; otherwise false</returns>
    public bool Equals(ConstrainedString<TConstraint> other)
        => _innerValue == other._innerValue;

    /// <inheritdoc cref="string.Equals(object)"/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is not null && _innerValue == obj.ToString();

    /// <inheritdoc cref="string.GetHashCode()"/>
    public override int GetHashCode()
        => _innerValue.GetHashCode();
    
    /// <inheritdoc cref="CharBuffer.IndexOf(char)"/>
    public int? IndexOf(char character)
    {
        var result = AsSpan.IndexOf(character);
        return result >= 0 ? result : null;
    }

    /// <inheritdoc cref="CharBuffer.IndexOf(ReadOnlySpan{char})"/>
    public int? IndexOf(ReadOnlySpan<char> substring)
    {
        var result = AsSpan.IndexOf(substring);
        return result >= 0 ? result : null;
    }
    
    /// <inheritdoc cref="string.Insert"/>
    public ConstrainedString<TConstraint> Insert(int startIndex, char value)
        => Create(_innerValue.Insert(startIndex, value.ToString()));

    /// <inheritdoc cref="string.Insert"/>
    public ConstrainedString<TConstraint> Insert(int startIndex, ReadOnlySpan<char> value)
        => Create(_innerValue.Insert(startIndex, value.ToString()));

    /// <inheritdoc cref="CharBuffer.LastIndexOf(char)"/>
    public int? LastIndexOf(char character)
    {
        var result = AsSpan.LastIndexOf(character);
        return result >= 0 ? result : null;
    }
    
    /// <inheritdoc cref="CharBuffer.LastIndexOf(ReadOnlySpan{char})"/>
    public int? LastIndexOf(ReadOnlySpan<char> substring)
    {
        var result = AsSpan.LastIndexOf(substring);
        return result >= 0 ? result : null;
    }
    
    /// <inheritdoc cref="string.PadLeft(int, char)"/>
    public ConstrainedString<TConstraint> PadLeft(int totalWidth, char paddingChar = ' ')
        => Create(_innerValue.PadLeft(totalWidth, paddingChar));
    
    /// <inheritdoc cref="string.PadRight(int, char)"/>
    public ConstrainedString<TConstraint> PadRight(int totalWidth, char paddingChar = ' ')
        => Create(_innerValue.PadRight(totalWidth, paddingChar));
    
    /// <inheritdoc cref="string.Remove(int, int)"/>
    public ConstrainedString<TConstraint> Remove(int startIndex, int? count = null)
    {
        count ??= Length - startIndex;
        return Create(_innerValue.Remove(startIndex, count.Value));
    }
    
    /// <inheritdoc cref="string.Replace(char, char)"/>
    public ConstrainedString<TConstraint> Replace(char oldCharacter, char newCharacter)
        => Create(_innerValue.Replace(oldCharacter, newCharacter));
    
    /// <inheritdoc cref="string.Replace(string, string)"/>
    public ConstrainedString<TConstraint> Replace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue)
        => Create(_innerValue.Replace(oldValue.ToString(), newValue.ToString()));
    
    /// <inheritdoc cref="string.StartsWith(char)"/>
    public bool StartsWith(char value)
        => Length > 0 && AsSpan[0] == value;

    /// <inheritdoc cref="string.StartsWith(string)"/>
    public bool StartsWith(ReadOnlySpan<char> value)
        => AsSpan.StartsWith(value);
    
    /// <inheritdoc cref="string.Substring(int, int)"/>
    public string Substring(int startIndex, int? length = null)
    {
        length ??= Length - startIndex;
        return AsSpan[startIndex..(startIndex + length.Value)].ToString();
    }
    
    /// <inheritdoc cref="string.ToLower(CultureInfo)"/>
    public ConstrainedString<TConstraint> ToLower(CultureInfo? cultureInfo = null)
        => Create(_innerValue.ToLower(cultureInfo));

    /// <inheritdoc cref="string.ToString()"/>
    public override string ToString()
        => _innerValue;
    
    /// <inheritdoc cref="string.ToUpper(CultureInfo)"/>
    public ConstrainedString<TConstraint> ToUpper(CultureInfo? cultureInfo = null)
        => Create(_innerValue.ToUpper(cultureInfo));
    
    /// <inheritdoc cref="string.Trim(char)"/>
    public ConstrainedString<TConstraint> Trim(char trimCharacter = ' ')
        => Create(_innerValue.Trim(trimCharacter));
    
    /// <inheritdoc cref="string.Trim(char[])"/>
    public ConstrainedString<TConstraint> Trim(ReadOnlySpan<char> trimChars)
        => Create(_innerValue.Trim(trimChars.ToArray()));
    
    /// <inheritdoc cref="string.TrimEnd(char)"/>
    public ConstrainedString<TConstraint> TrimEnd(char trimCharacter = ' ')
        => Create(_innerValue.TrimEnd(trimCharacter));
    
    /// <inheritdoc cref="string.TrimEnd(char[])"/>
    public ConstrainedString<TConstraint> TrimEnd(ReadOnlySpan<char> trimChars)
        => Create(_innerValue.TrimEnd(trimChars.ToArray()));
    
    /// <inheritdoc cref="string.TrimStart(char)"/>
    public ConstrainedString<TConstraint> TrimStart(char trimCharacter = ' ')
        => Create(_innerValue.TrimStart(trimCharacter));
    
    /// <inheritdoc cref="string.TrimStart(char[])"/>
    public ConstrainedString<TConstraint> TrimStart(ReadOnlySpan<char> trimChars)
        => Create(_innerValue.TrimStart(trimChars.ToArray()));
    
    /// <inheritdoc cref="string.TryCopyTo(Span{char})"/>
    public bool TryCopyTo(Span<char> destination)
        => _innerValue.TryCopyTo(destination);

    /// <summary>
    /// Compares two instances of constrained strings for equality
    /// </summary>
    /// <param name="left">the first string to compare</param>
    /// <param name="right">the second string to compare</param>
    /// <returns>true, if both instances are equal; otherwise false</returns>
    public static bool operator ==(ConstrainedString<TConstraint> left, ConstrainedString<TConstraint> right)
        => left._innerValue == right._innerValue;

    /// <summary>
    /// Compares two instances of constrained strings for equality
    /// </summary>
    /// <param name="left">the first string to compare</param>
    /// <param name="right">the second string to compare</param>
    /// <returns>true, if both instances are not equal; otherwise false</returns>
    public static bool operator !=(ConstrainedString<TConstraint> left, ConstrainedString<TConstraint> right)
        => !(left == right);
    
    /// <summary>
    /// implicitly converts this instance to a string
    /// </summary>
    /// <param name="constrainedString">the instance to convert</param>
    /// <returns>This instance as a string</returns>
    public static implicit operator string(ConstrainedString<TConstraint> constrainedString)
        => constrainedString._innerValue;
    
    /// <summary>
    /// implicitly converts this instance to a span of characters
    /// </summary>
    /// <param name="constrainedString">the instance to convert</param>
    /// <returns>This instance as a span of characters</returns>
    public static implicit operator ReadOnlySpan<char>(ConstrainedString<TConstraint> constrainedString)
        => constrainedString._innerValue;
    
    /// <summary>
    /// Explicitly converts a string to a constrained string, verifying its constraint.
    /// For more information see <see cref="Create"/>
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>An instance of ConstrainedString with the same content as value</returns>
    public static explicit operator ConstrainedString<TConstraint>(string value)
        => Create(value);

    /// <summary>
    /// Explicitly converts a span of bytes to a constrained string, verifying its constraint.
    /// For more information see <see cref="Create"/>
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>An instance of ConstrainedString with the same content as value</returns>
    public static explicit operator ConstrainedString<TConstraint>(ReadOnlySpan<char> value)
        => Create(value.ToString());
}