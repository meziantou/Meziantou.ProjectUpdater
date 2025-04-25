using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Meziantou.ProjectUpdater.GitHub.Client.Internals;

internal sealed class HttpResponseStream : Stream
{
    private readonly Stream _stream;
    private readonly HttpResponseMessage _message;

    internal HttpResponseStream(Stream stream, HttpResponseMessage httpResponseMessage)
    {
        _stream = stream;
        _message = httpResponseMessage;
    }

    public string? FileName => RemoveQuote(_message.Content.Headers.ContentDisposition?.FileName);
    public MediaTypeHeaderValue? ContentType => _message.Content.Headers.ContentType;

    [return: NotNullIfNotNull(nameof(fileName))]
    private static string? RemoveQuote(string? fileName)
    {
        if (fileName is null || fileName.Length < 2)
            return fileName;

        if (fileName[0] == '"' && fileName[^1] == '"')
            return fileName[1..^1];

        return fileName;
    }

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        _message.Dispose();
        _stream.Dispose();
        base.Dispose(disposing);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _stream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _stream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override bool CanTimeout => _stream.CanTimeout;

    public override void Close()
    {
        _stream.Close();
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _stream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _stream.EndWrite(asyncResult);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _stream.FlushAsync(cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override int ReadByte()
    {
        return _stream.ReadByte();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    public override void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    public override int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        _stream.CopyTo(destination, bufferSize);
    }

    [SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "We just wrap the type")]
    public override ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }

    public override int Read(Span<byte> buffer)
    {
        return _stream.Read(buffer);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _stream.ReadAsync(buffer, cancellationToken);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _stream.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _stream.WriteAsync(buffer, cancellationToken);
    }
}
