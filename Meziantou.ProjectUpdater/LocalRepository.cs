using System.Text;
using Meziantou.Framework;
using Meziantou.Framework.Globbing;

namespace Meziantou.ProjectUpdater;

public sealed class LocalRepository : IAsyncDisposable
{
    private readonly TemporaryDirectory _temporaryDirectory;

    internal LocalRepository(TemporaryDirectory temporaryDirectory)
    {
        _temporaryDirectory = temporaryDirectory;
    }

    public async Task UpdateFileAsync(string path, Func<byte[], byte[]> func, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var newBytes = func(bytes);
        if (!bytes.SequenceEqual(newBytes))
            await File.WriteAllBytesAsync(fullPath, newBytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateFileAsync(string path, Func<string, string> func, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        var encoding = await FileUtilities.GetEncodingAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var text = await File.ReadAllTextAsync(fullPath, encoding, cancellationToken).ConfigureAwait(false);
        var newText = func(text);
        if (text != newText)
            await File.WriteAllTextAsync(fullPath, newText, encoding, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddFileAsync(string path, byte[] content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
    }

    public Task AddFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        return AddFileAsync(path, content, Encoding.Default, cancellationToken);
    }

    public async Task AddFileAsync(string path, string content, Encoding encoding, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        await File.WriteAllTextAsync(fullPath, content, encoding, cancellationToken).ConfigureAwait(false);
    }

    public IEnumerable<FullPath> FindFile(string globPattern, GlobOptions options = GlobOptions.None)
    {
        var glob = Glob.Parse(globPattern, options);
        return glob.EnumerateFiles(RootPath).Select(FullPath.FromPath);
    }

    private FullPath GetFullPath(string path)
    {
        return RootPath / path;
    }

    public FullPath RootPath => _temporaryDirectory.FullPath;

    public ValueTask DisposeAsync() => _temporaryDirectory.DisposeAsync();
}