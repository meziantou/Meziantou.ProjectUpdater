using System.Text;
using Meziantou.Framework;
using Meziantou.Framework.Globbing;
using Microsoft.Extensions.Logging;

namespace Meziantou.ProjectUpdater;

public sealed class LocalRepository : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly TemporaryDirectory _temporaryDirectory;
    private readonly Project _project;
    private readonly ProjectUpdaterOptions _options;
    private readonly CancellationToken _cancellationToken;

    internal LocalRepository(ILogger logger, TemporaryDirectory temporaryDirectory, Project project, ProjectUpdaterOptions options, CancellationToken cancellationToken)
    {
        _logger = logger;
        _temporaryDirectory = temporaryDirectory;
        _project = project;
        _options = options;
        _cancellationToken = cancellationToken;
    }

    public async Task CloneAsync()
    {
        Log.CloningProject(_logger, _project.Id, _project.Name);
        await _project.CloneAsync(_temporaryDirectory.FullPath, _options, _cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateFileAsync(string path, Func<byte[], byte[]> func)
    {
        var fullPath = GetFullPath(path);
        var bytes = await File.ReadAllBytesAsync(fullPath, _cancellationToken).ConfigureAwait(false);
        var newBytes = func(bytes);
        if (!bytes.SequenceEqual(newBytes))
        {
            await File.WriteAllBytesAsync(fullPath, newBytes, _cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateFileAsync(string path, Func<string, string> func)
    {
        var fullPath = GetFullPath(path);
        var encoding = await FileUtilities.GetEncodingAsync(fullPath, _cancellationToken).ConfigureAwait(false);
        var text = await File.ReadAllTextAsync(fullPath, encoding, _cancellationToken).ConfigureAwait(false);
        var newText = func(text);
        if (text != newText)
        {
            await File.WriteAllTextAsync(fullPath, newText, encoding, _cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task AddFileAsync(string path, byte[] content)
    {
        var fullPath = GetFullPath(path);
        await File.WriteAllBytesAsync(fullPath, content, _cancellationToken).ConfigureAwait(false);
    }

    public Task AddFileAsync(string path, string content)
    {
        return AddFileAsync(path, content, Encoding.Default);
    }

    public async Task AddFileAsync(string path, string content, Encoding encoding)
    {
        var fullPath = GetFullPath(path);
        await File.WriteAllTextAsync(fullPath, content, encoding, _cancellationToken).ConfigureAwait(false);
    }

    public IEnumerable<FullPath> FindFile(string globPattern, GlobOptions options = GlobOptions.None)
    {
        var glob = Glob.Parse(globPattern, options);
        return glob.EnumerateFiles(RootPath).Select(FullPath.FromPath).Where(IncludeFile);
    }

    private FullPath GetFullPath(string path)
    {
        return RootPath / path;
    }

    public FullPath RootPath => _temporaryDirectory.FullPath;

    public ValueTask DisposeAsync() => _temporaryDirectory.DisposeAsync();

    private bool IncludeFile(FullPath path)
    {
        var gitPath = RootPath / ".git";
        return !path.IsChildOf(gitPath);
    }

    public override string ToString() => "Repo: " + _project.Name;
}