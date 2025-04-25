using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Meziantou.ProjectUpdater.GitHub.Client.Internals;

internal ref partial struct UrlBuilder
{
    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public UrlBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public UrlBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public UrlBuilder(UrlInterpolatedStringHandler handler)
    {
        _arrayToReturnToPool = null;
        _chars = null;
        _pos = 0;
        AppendRaw(handler);
    }

    public int Length
    {
        readonly get => _pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _chars.Length);
            _pos = value;
        }
    }

    public readonly int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _chars.Length)
            Grow(capacity - _pos);
    }

    public readonly ref char GetPinnableReference() => ref MemoryMarshal.GetReference(_chars);

    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }
        return ref MemoryMarshal.GetReference(_chars);
    }

    public readonly ref char this[int index]
    {
        get
        {
            Debug.Assert(index < _pos);
            return ref _chars[index];
        }
    }

    public override string ToString()
    {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    public readonly Span<char> RawChars => _chars;

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars[.._pos].TryCopyTo(destination))
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }
        else
        {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendRaw(char c)
    {
        var pos = _pos;
        if ((uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendRaw(string? s)
    {
        if (s is null)
            return;

        var pos = _pos;
        if (s.Length is 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendRawSlow(s);
        }
    }

    private void AppendRawSlow(string s)
    {
        var pos = _pos;
        if (pos > _chars.Length - s.Length)
            Grow(s.Length);

        s.AsSpan().CopyTo(_chars[pos..]);
        _pos += s.Length;
    }

    private void AppendRaw(ReadOnlySpan<char> value)
    {
        var pos = _pos;
        if (pos > _chars.Length - value.Length)
            Grow(value.Length);

        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    public void AppendRaw(UrlInterpolatedStringHandler handler)
    {
        AppendRaw(handler.Text);
        handler.Clear();
    }

    public void AppendQuery(string name, string value)
    {
        AppendQuerySeparator();
        AppendRaw(name);
        AppendRaw('=');
        AppendEncoded(value);
    }

    public void AppendQuery(string name, int value)
    {
        AppendQuerySeparator();
        AppendRaw(name);
        AppendRaw('=');
        AppendEncoded(value.ToString(CultureInfo.InvariantCulture));
    }

    private void AppendEncoded(string value)
    {
        AppendRaw(Uri.EscapeDataString(value));
    }

    private void AppendQuerySeparator()
    {
        if (RawChars.IndexOf('?') >= 0)
            AppendRaw('&');
        else
        {
            AppendRaw('?');
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        AppendRaw(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

        var poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_pos + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars[.._pos].CopyTo(poolArray);

        var toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        var toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn is not null)
            ArrayPool<char>.Shared.Return(toReturn);
    }
}
