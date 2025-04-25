using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Meziantou.ProjectUpdater.GitHub.Client.Internals;

// Copied and adapted from DefaultInterpolatedStringHandler
// https://github.com/dotnet/runtime/blob/2f43856e78faebe7347fdd76acc26de7ffaec539/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/DefaultInterpolatedStringHandler.cs
[InterpolatedStringHandler]
internal ref struct UrlInterpolatedStringHandler
{
    private const int MaxLength = 0x3FFFFFDF;
    private const int GuessedLengthPerHole = 11;
    private const int MinimumArrayPoolLength = 256;

    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public UrlInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _chars = _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount));
        _pos = 0;
    }

    public UrlInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> initialBuffer)
    {
        _chars = initialBuffer;
        _arrayToReturnToPool = null;
        _pos = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // becomes a constant when inputs are constant
    internal static int GetDefaultLength(int literalLength, int formattedCount) =>
        Math.Max(MinimumArrayPoolLength, literalLength + formattedCount * GuessedLengthPerHole);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    /// <returns>The built string.</returns>
    public override readonly string ToString() => new(Text);

    public string ToStringAndClear()
    {
        var result = new string(Text);
        Clear();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        var toReturn = _arrayToReturnToPool;
        this = default; // defensive clear
        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }

    /// <summary>Gets a span of the written characters thus far.</summary>
    internal readonly ReadOnlySpan<char> Text => _chars[.._pos];

    /// <summary>Writes the specified string to the handler.</summary>
    /// <param name="value">The string to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value)
    {
        if (value.TryCopyTo(_chars[_pos..]))
            _pos += value.Length;
        else
        {
            GrowThenCopyString(value);
        }
    }

    public void AppendFormatted(int value)
    {
        int charsWritten;
        while (!value.TryFormat(_chars[_pos..], out charsWritten, default, CultureInfo.InvariantCulture))
        {
            Grow();
        }

        _pos += charsWritten;
    }

    public void AppendFormatted(string? value)
    {
        if (value is null)
            return;

        AppendLiteral(Uri.EscapeDataString(value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopyString(string value)
    {
        Grow(value.Length);
        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalChars)
    {
        // This method is called when the remaining space (_chars.Length - _pos) is
        // insufficient to store a specific number of additional characters.  Thus, we
        // need to grow to at least that new total. GrowCore will handle growing by more
        // than that if possible.
        Debug.Assert(additionalChars > _chars.Length - _pos);
        GrowCore((uint)_pos + (uint)additionalChars);
    }

    /// <summary>Grows the size of <see cref="_chars"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)] // keep consumers as streamlined as possible
    private void Grow()
    {
        // This method is called when the remaining space in _chars isn't sufficient to continue
        // the operation.  Thus, we need at least one character beyond _chars.Length.  GrowCore
        // will handle growing by more than that if possible.
        GrowCore((uint)_chars.Length + 1);
    }

    /// <summary>Grow the size of <see cref="_chars"/> to at least the specified <paramref name="requiredMinCapacity"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // but reuse this grow logic directly in both of the above grow routines
    private void GrowCore(uint requiredMinCapacity)
    {
        // We want the max of how much space we actually required and doubling our capacity (without going beyond the max allowed length). We
        // also want to avoid asking for small arrays, to reduce the number of times we need to grow, and since we're working with unsigned
        // ints that could technically overflow if someone tried to, for example, append a huge string to a huge string, we also clamp to int.MaxValue.
        // Even if the array creation fails in such a case, we may later fail in ToStringAndClear.

        var newCapacity = Math.Max(requiredMinCapacity, Math.Min((uint)_chars.Length * 2, MaxLength));
        var arraySize = (int)Math.Clamp(newCapacity, MinimumArrayPoolLength, int.MaxValue);

        var newArray = ArrayPool<char>.Shared.Rent(arraySize);
        _chars[.._pos].CopyTo(newArray);

        var toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = newArray;

        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }
}
