using System.Buffers;
using System.Globalization;
using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

/// <summary>
/// An alternative to <see cref="System.Text.StringBuilder"/> in the standard library that uses a char array as its
/// backing store and offers all functions that a typical string offers. While avoiding frequent allocations
/// with a generous memory allocation strategy. This means that instances of <see cref="CharBuffer"/> may be bigger
/// than necessary while working with smaller strings.
/// </summary>
/// <remarks>
/// It is recommended to avoid <see cref="System.Text.StringBuilder"/> instances while working with large strings
/// as the implementation uses a linked list of chunks. That implementation was good enough if you consider that
/// .NET didn't have any efficient way of handling large chunks of consecutive memory. After the introduction of the
/// Large-Object-Heap it is almost always recommended to use <see cref="CharBuffer"/> instead.
/// </remarks>
[ExclusiveMemoryAccess(Reason = "This type is easily mutable by design. Any multi-threading could lead to undefined " +
                                "behavior and allocation errors")]
public sealed class CharBuffer : IDisposable, IEquatable<CharBuffer>
{
    private const int MinCapacityIncrement = 128;
    
    /// <summary>
    /// Creates a new instance of an empty <see cref="CharBuffer"/> with at least the given <paramref name="capacity"/>
    /// </summary>
    /// <param name="capacity">
    /// The capacity that this instance of CharBuffer starts with at least.
    /// Providing zero or any negative value will initialize the instance with a default capacity instead.
    /// </param>
    /// <param name="pool">The memory pool to use for buffer allocation with the shared pool as the default</param>
    /// <exception cref="ArgumentOutOfRangeException">if capacity is negative</exception>
    public CharBuffer(int capacity, MemoryPool<char>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
        
        _pool = pool ?? MemoryPool<char>.Shared;
        _buffer = _pool.Rent(capacity);
    }

    /// <summary>
    /// Creates a new instance with the given <paramref name="value"/> as its content.
    /// </summary>
    /// <param name="value">The string content to initialize this instance to</param>
    /// <param name="pool">The memory pool to use for buffer allocation with the shared pool as the default</param>
    public CharBuffer(ReadOnlySpan<char> value, MemoryPool<char>? pool = null)
    {
        _pool = pool ?? MemoryPool<char>.Shared;
        _buffer = _pool.Rent(value.Length);
        value.CopyTo(_buffer.Memory.Span);
        _length = value.Length;
    }

    /// <summary>
    /// Deconstructs this instance by disposing the internal buffer
    /// </summary>
    ~CharBuffer()
    {
        _buffer.Dispose();
    }
    
    private readonly MemoryPool<char> _pool;
    
    private IMemoryOwner<char> _buffer;

    private int _length;
    
    private volatile bool _disposed;
    
    /// <summary>
    /// Frees the internally allocated buffer
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        _buffer.Dispose();
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Provides the content of this <see cref="CharBuffer"/> instance as a <see cref="ReadOnlySpan{T}"/> of characters.
    /// </summary>
    public ReadOnlySpan<char> AsSpan
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
            
            return _buffer.Memory.Span[.._length];
        }
    }

    private Span<char> AvailableBuffer => _buffer.Memory.Span[_length..];
    
    /// <summary>
    /// The current capacity in UTF-16 units of this <see cref="CharBuffer"/>. You can use <see cref="EnsureCapacity"/>
    /// to reserve capacity in anticipation of a higher memory need.
    /// </summary>
    public int Capacity
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
            
            return _buffer.Memory.Length;
        }
    }

    /// <summary>
    /// The current length in UTF-16 units of this <see cref="CharBuffer"/>
    /// </summary>
    public int Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
            
            return _length;
        }
    }

    /// <summary>
    /// Gets or sets the character at the specified <paramref name="index"/>
    /// </summary>
    /// <param name="index">The zero-based index of the specified character in this instance</param>
    public char this[int index]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
            
            return AsSpan[index];
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
            
            _buffer.Memory.Span[.._length][index] = value;
        }
    }

    /// <summary>
    /// Appends the specified <paramref name="character"/> to the end of this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="character">The UTF-16 codepoint to append</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Append(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        EnsureCapacity(_length + 1);
        _buffer.Memory.Span[_length++] = character;
        return this;
    }

    /// <summary>
    /// Appends the specified <paramref name="text"/> to the end of this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="text">The span of characters to append</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Append(ReadOnlySpan<char> text)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (text.Length == 0)
            return this;
        
        EnsureCapacity(_length + text.Length);
        text.CopyTo(AvailableBuffer);
        _length += text.Length;
        return this;
    }
    
    /// <summary>
    /// Appends the specified <paramref name="value"/> with the specified <paramref name="format"/> to the
    /// end of this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="value">The formattable value to append</param>
    /// <param name="format">The format string to use</param>
    /// <param name="provider">optionally, the format provider to use. e.g. <see cref="CultureInfo.InvariantCulture"/></param>
    /// <typeparam name="TValue">The type of value to append</typeparam>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Append<TValue>(TValue value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        where TValue : ISpanFormattable
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));

        int charsWritten;

        while (!value.TryFormat(AvailableBuffer, out charsWritten, format, provider))
        {
            EnsureCapacity(Capacity + AllocationLimit.InChars);
        }
        
        _length += charsWritten;
        return this;
    }

    /// <summary>
    /// Deletes all content of this instance leaving its capacity intact.
    /// </summary>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        _length = 0;
        return this;
    }

    /// <summary>
    /// Searches this <see cref="CharBuffer"/> instance for any occurrence of the specified <paramref name="character"/>
    /// </summary>
    /// <param name="character">The UTF-16 codepoint to search for</param>
    /// <returns>true, if the specified character occurs at least once, otherwise false</returns>
    public bool Contains(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return AsSpan.Contains(character);
    }

    /// <summary>
    /// Searches this <see cref="CharBuffer"/> instance for any occurrence of the specified <paramref name="text"/>
    /// </summary>
    /// <param name="text">The string to search for</param>
    /// <returns>true, if the specified string occurs at least once, otherwise false</returns>
    public bool Contains(ReadOnlySpan<char> text)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return AsSpan.Contains(text, StringComparison.Ordinal);
    }

    /// <inheritdoc cref="String.CopyTo(Span{char})"/>
    public CharBuffer CopyTo(Span<char> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        AsSpan.CopyTo(destination);
        return this;
    }

    /// <inheritdoc cref="string.EndsWith(char)"/>
    public bool EndsWith(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return _length > 0 && AsSpan[^1] == character;
    }

    /// <summary>
    /// Determines whether the end of this string instance matches the specified text.
    /// </summary>
    /// <param name="text">The text to compare the end of this instance with</param>
    /// <returns>true if value matches the end of this instance; otherwise, false</returns>
    public bool EndsWith(ReadOnlySpan<char> text)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return AsSpan.EndsWith(text);
    }

    /// <summary>
    /// Ensures that this instance has at least the specified capacity,
    /// reallocating the internal buffer, if necessary. 
    /// </summary>
    /// <param name="capacity">The capacity in UTF-16 codepoints to ensure</param>
    public void EnsureCapacity(int capacity)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (Capacity >= capacity)
            return;
        
        var newCapacity = Capacity + Math.Max(capacity - Capacity, MinCapacityIncrement);
        var newBuffer = _pool.Rent(newCapacity);
        _buffer.Memory.CopyTo(newBuffer.Memory);
        _buffer.Dispose();
        _buffer = newBuffer;
    }
    
    /// <inheritdoc cref="object.Equals(object)"/>
    public override bool Equals(object? obj)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return ReferenceEquals(this, obj) || obj is CharBuffer other && Equals(other);
    }

    /// <summary>
    /// Determines whether the specified <see cref="CharBuffer"/> has equal content to this instance.
    /// </summary>
    /// <param name="other">The other instance to compare against</param>
    /// <returns>true if content matches; otherwise, false</returns>
    public bool Equals(CharBuffer? other)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (other is null)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        return AsSpan.Equals(other.AsSpan, StringComparison.Ordinal);
    }

    /// <summary>
    /// Generates a hash based on the content of this instance. Please note that this hashcode will change every time
    /// the content of this instance changes.
    /// </summary>
    /// <returns>a hashcode based on the current content of this instance</returns>
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        // implementation of FNV-1a hash function
        uint hash = 2166136261;
        const uint prime = 16777619;
        Span<byte> buffer = stackalloc byte[sizeof(char)];
        
        foreach (var character in AsSpan)
        {
            BitConverter.TryWriteBytes(buffer, character); // this process always succeeds

            hash ^= buffer[0];
            hash *= prime;
            
            hash ^= buffer[1];
            hash *= prime;
        }

        unsafe
        {
            var hashPointer = &hash;
            return *(int*)hashPointer;
        }
    }
    
    /// <summary>
    /// Searches this instance from the left for the first occurrence of the specified character
    /// and returns its index
    /// </summary>
    /// <param name="character">The UTF-16 codepoint to search for</param>
    /// <returns>the zero-based index of the specified character; otherwise, null</returns>
    public int? IndexOf(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var result = AsSpan.IndexOf(character);
        return result >= 0 ? result : null;
    }

    /// <summary>
    /// Searches this instance from the left for the first occurrence of the specified substring
    /// and returns its index 
    /// </summary>
    /// <param name="substring">The string to search for</param>
    /// <returns>the zero-based index of the specified substring; otherwise, null</returns>
    public int? IndexOf(ReadOnlySpan<char> substring)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var result = AsSpan.IndexOf(substring);
        return result >= 0 ? result : null;
    }
    
    /// <summary>
    /// Inserts the specified character at the given index into this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="index">The zero-based index to insert the character</param>
    /// <param name="character">The UTF-16 codepoint to insert</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentOutOfRangeException">the index is less than 0 or larger than length</exception>
    public CharBuffer Insert(int index, char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (index < 0 || index > _length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        EnsureCapacity(_length + 1);
        AsSpan[index..].CopyTo(_buffer.Memory.Span[(index + 1)..]);
        _buffer.Memory.Span[index] = character;
        _length++;
        return this; 
    }

    /// <summary>
    /// Inserts the specified text at the given index into this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="index">The zero-based index to insert the text</param>
    /// <param name="text">The text to insert</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentOutOfRangeException">the index is less than 0 or larger than length</exception>
    public CharBuffer Insert(int index, ReadOnlySpan<char> text)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (index < 0 || index > _length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        if (text.Length == 0)
            return this;
        
        EnsureCapacity(_length + text.Length);
        AsSpan[index..].CopyTo(_buffer.Memory.Span[(index + text.Length)..]);
        text.CopyTo(_buffer.Memory.Span[index..]);
        _length += text.Length;
        return this;
    }

    /// <summary>
    /// Inserts the specified value at the given index into this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="index">The zero-based index to insert the text</param>
    /// <param name="value">The text to insert</param>
    /// <param name="format">The format string to use</param>
    /// <param name="provider">optionally, the format provider to use</param>
    /// <typeparam name="TValue">The type of the value to insert</typeparam>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentException">The formatted value is longer than 256 UTF-16 codepoints</exception>
    public CharBuffer Insert<TValue>(
        int index,
        TValue value,
        ReadOnlySpan<char> format,
        IFormatProvider? provider = null
    ) where TValue : ISpanFormattable
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        Span<char> valueBuffer = stackalloc char[AllocationLimit.InChars];

        if (!value.TryFormat(valueBuffer, out var charsWritten, format, provider))
            throw new ArgumentException($"value is longer than {AllocationLimit.InChars} characters long");
        
        return Insert(index, valueBuffer[..charsWritten]);
    }
    
    /// <summary>
    /// Searches for the last occurence of the specified character in this instance and returns its index.
    /// </summary>
    /// <param name="character">The UTF-16 codepoint to search for</param>
    /// <returns>The zero-based index of the last occurrence if found; otherwise null</returns>
    public int? LastIndexOf(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var result = AsSpan.LastIndexOf(character);
        return result >= 0 ? result : null;
    }
    
    /// <summary>
    /// Searches for the last occurence of the specified substring in this instance and returns its index.
    /// </summary>
    /// <param name="substring">The substring to search for</param>
    /// <returns>The zero-based index of the last occurrence if found; otherwise null</returns>
    public int? LastIndexOf(ReadOnlySpan<char> substring)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var result = AsSpan.LastIndexOf(substring);
        return result >= 0 ? result : null;
    }

    /// <summary>
    /// Adds the specified amount of padding characters to the beginning of this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="totalWidth">The total amount of padding characters to add</param>
    /// <param name="paddingCharacter">The UTF-16 codepoint to add with space as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentOutOfRangeException">if totalWidth is negative</exception>
    public CharBuffer PadLeft(int totalWidth, char paddingCharacter = ' ')
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        ArgumentOutOfRangeException.ThrowIfNegative(totalWidth, nameof(totalWidth));
        
        EnsureCapacity(_length + totalWidth);
        AsSpan.CopyTo(_buffer.Memory.Span[totalWidth..]);
        _buffer.Memory.Span[..totalWidth].Fill(paddingCharacter);
        _length += totalWidth;
        return this;
    }
    
    /// <summary>
    /// Adds the specified amount of padding characters to the end of this instance.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="totalWidth">The total amount of padding characters to add</param>
    /// <param name="paddingCharacter">The UTF-16 codepoint to add with space as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentOutOfRangeException">if totalWidth is negative</exception>
    public CharBuffer PadRight(int totalWidth, char paddingCharacter = ' ')
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        ArgumentOutOfRangeException.ThrowIfNegative(totalWidth, nameof(totalWidth));
        
        EnsureCapacity(_length + totalWidth);
        AvailableBuffer[..totalWidth].Fill(paddingCharacter);
        _length += totalWidth;
        return this;
    }

    /// <summary>
    /// Removes the specified number of characters from this instance starting at the specified index.
    /// </summary>
    /// <param name="startIndex">the zero-based index to start removing from</param>
    /// <param name="count">optionally, the number of characters to remove with the rest of the buffer as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    /// <exception cref="ArgumentOutOfRangeException">if either startIndex or count are out of the valid range</exception>
    public CharBuffer Remove(int startIndex, int? count = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(startIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, _length, nameof(startIndex));

        if (count is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count.Value, nameof(count));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex + count.Value, _length, nameof(count));
        }
        
        count ??= _length - startIndex;
        AsSpan[(startIndex + count.Value)..].CopyTo(_buffer.Memory.Span[startIndex..]);
        _length -= count.Value;
        return this;
    }

    /// <summary>
    /// Replaces all occurrences of the specified character with a replacement.
    /// </summary>
    /// <param name="oldCharacter">The UTF-16 codepoint to replace</param>
    /// <param name="newCharacter">The UTF-16 codepoint to be used as a replacement</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Replace(char oldCharacter, char newCharacter)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        _buffer.Memory.Span[.._length].Replace(oldCharacter, newCharacter);
        return this;
    }

    /// <summary>
    /// Replaces all occurrences of the specified character with a replacement.
    /// Reallocating the internal buffer if necessary.
    /// </summary>
    /// <param name="oldText">The substring to replace</param>
    /// <param name="newText">The substring to act as a replacement</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Replace(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (oldText.Length == 0)
            return this;
        
        var index = AsSpan.IndexOf(oldText);
        var sizeDifference = newText.Length - oldText.Length;

        while (index >= 0)
        {
            EnsureCapacity(_length + sizeDifference);
            AsSpan[(index + oldText.Length)..].CopyTo(_buffer.Memory.Span[(index + oldText.Length + sizeDifference)..]);
            newText.CopyTo(_buffer.Memory.Span[index..]);
            _length += sizeDifference;
            
            var nextIndex = AsSpan[(index + newText.Length)..].IndexOf(oldText);

            if (nextIndex >= 0)
                nextIndex += index + newText.Length;
            
            index = nextIndex;
        }

        return this;
    }

    /// <summary>
    /// Determines if this instance starts with the specified character.
    /// </summary>
    /// <param name="character">The UTF-16 codepoint to check against</param>
    /// <returns>true, if this instance starts with the character. otherwise false</returns>
    public bool StartsWith(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return _length > 0 && AsSpan[0] == character;
    }

    /// <summary>
    /// Determines if this instance starts with the specified text.
    /// </summary>
    /// <param name="text">The substring to check against</param>
    /// <returns>true, if this instance starts with the substring. otherwise false</returns>
    public bool StartsWith(ReadOnlySpan<char> text)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return AsSpan.StartsWith(text);
    }

    /// <summary>
    /// Extracts the specified substring from this instance.
    /// </summary>
    /// <param name="startIndex">The zero-based index of the substring</param>
    /// <param name="count">optionally, the amount of characters the substring consists of with the rest as a default</param>
    /// <returns>The substring as its own <see cref="string"/> instance</returns>
    /// <exception cref="ArgumentOutOfRangeException">either startIndex or count are not in a valid range</exception>
    public string SubString(int startIndex, int? count = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex, nameof(startIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, _length, nameof(startIndex));

        if (count is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count.Value, nameof(count));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex + count.Value, _length, nameof(count));
        }
        
        count ??= _length - startIndex;
        return AsSpan[startIndex..(startIndex + count.Value)].ToString();
    }

    /// <summary>
    /// Converts all characters in this instance to its lowercase representation. 
    /// </summary>
    /// <param name="cultureInfo">optionally, the specific culture to use for the conversion</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer ToLower(CultureInfo? cultureInfo = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        cultureInfo ??= CultureInfo.InvariantCulture;
        var bufferSpan = _buffer.Memory.Span[.._length];
        
        for (var index = 0; index < _length; index++)
        {
            var character = bufferSpan[index];
            var lowerChar = char.ToLower(character, cultureInfo);
            
            if (character != lowerChar)
                bufferSpan[index] = lowerChar;
        }

        return this;
    }

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        return AsSpan.ToString();
    }

    /// <summary>
    /// Converts all characters in this instance to its uppercase representation. 
    /// </summary>
    /// <param name="cultureInfo">optionally, the specific culture to use for the conversion</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer ToUpper(CultureInfo? cultureInfo = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        cultureInfo ??= CultureInfo.InvariantCulture;
        var bufferSpan = _buffer.Memory.Span[.._length];
        
        for (var index = 0; index < _length; index++)
        {
            var character = bufferSpan[index];
            var lowerChar = char.ToUpper(character, cultureInfo);
            
            if (character != lowerChar)
                bufferSpan[index] = lowerChar;
        }

        return this;
    }

    /// <summary>
    /// Trims the specified character from the beginning and end of this instance.
    /// </summary>
    /// <param name="trimCharacter">The UTF-16 codepoint to trim with space as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Trim(char trimCharacter = ' ')
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.Trim(trimCharacter);

        if (trimmedSpan.Length == _length)
            return this;
        
        trimmedSpan.CopyTo(_buffer.Memory.Span);
        _length -= _length - trimmedSpan.Length;
        return this;
    }

    /// <summary>
    /// Trims the specified characters from the beginning and end of this instance.
    /// </summary>
    /// <param name="trimCharacters">The UTF-16 codepoints to trim</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer Trim(ReadOnlySpan<char> trimCharacters)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.Trim(trimCharacters);

        if (trimmedSpan.Length == _length)
            return this;
        
        trimmedSpan.CopyTo(_buffer.Memory.Span);
        _length -= _length - trimmedSpan.Length;
        return this;
    }
    
    /// <summary>
    /// Trims the specified character from the end of this instance.
    /// </summary>
    /// <param name="trimCharacter">The UTF-16 codepoint to trim with space as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer TrimEnd(char trimCharacter = ' ')
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.TrimEnd(trimCharacter);

        if (trimmedSpan.Length == _length)
            return this;
        
        _length -= _length - trimmedSpan.Length;
        return this;
    }

    /// <summary>
    /// Trims the specified characters from the end of this instance.
    /// </summary>
    /// <param name="trimCharacters">The UTF-16 codepoints to trim</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer TrimEnd(ReadOnlySpan<char> trimCharacters)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.TrimEnd(trimCharacters);

        if (trimmedSpan.Length == _length)
            return this;
        
        _length -= _length - trimmedSpan.Length;
        return this;
    }
    
    /// <summary>
    /// Trims the specified character from the beginning of this instance.
    /// </summary>
    /// <param name="trimCharacter">The UTF-16 codepoint to trim with space as a default</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer TrimStart(char trimCharacter = ' ')
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.TrimStart(trimCharacter);

        if (trimmedSpan.Length == _length)
            return this;
        
        trimmedSpan.CopyTo(_buffer.Memory.Span);
        _length -= _length - trimmedSpan.Length;
        return this;
    }

    /// <summary>
    /// Trims the specified characters from the beginning of this instance.
    /// </summary>
    /// <param name="trimCharacters">The UTF-16 codepoints to trim</param>
    /// <returns>a reference to this instance to chain calls together</returns>
    public CharBuffer TrimStart(ReadOnlySpan<char> trimCharacters)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        var trimmedSpan = AsSpan.TrimStart(trimCharacters);

        if (trimmedSpan.Length == _length)
            return this;
        
        trimmedSpan.CopyTo(_buffer.Memory.Span);
        _length -= _length - trimmedSpan.Length;
        return this;
    }

    /// <inheritdoc cref="string.TryCopyTo"/>
    public bool TryCopyTo(Span<char> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CharBuffer));
        
        if (destination.Length < _length)
            return false;

        CopyTo(destination);
        return true;
    }
    
    /// <summary>
    /// implicitly converts this instance to a string
    /// </summary>
    /// <param name="instance">the instance to convert</param>
    /// <returns>This instance as a string</returns>
    public static explicit operator string(CharBuffer instance)
        => instance.ToString();
    
    /// <summary>
    /// implicitly converts this instance to a span of characters
    /// </summary>
    /// <param name="instance">the instance to convert</param>
    /// <returns>This instance as a span of characters</returns>
    public static implicit operator ReadOnlySpan<char>(CharBuffer instance)
        => instance.AsSpan;
}