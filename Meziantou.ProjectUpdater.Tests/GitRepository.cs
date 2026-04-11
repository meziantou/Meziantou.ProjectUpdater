using System.Text;
using Meziantou.Framework;

namespace Meziantou.ProjectUpdater.Tests;
internal sealed class GitRepository : IAsyncDisposable
{
    public static readonly IReadOnlyCollection<KeyValuePair<string, string>> DefaultConfiguration = [
        KeyValuePair.Create("user.name", "project_updater"),
        KeyValuePair.Create("user.username", "project_updater"),
        KeyValuePair.Create("user.email", "project_updater@example.com"),

        KeyValuePair.Create("user.signingkey", ""),
        KeyValuePair.Create("commit.gpgsign", "false"),
        KeyValuePair.Create("pull.rebase", "true"),
        KeyValuePair.Create("fetch.prune", "true"),
        KeyValuePair.Create("core.autocrlf", "false"),
        KeyValuePair.Create("core.longpaths", "true"),
        KeyValuePair.Create("rebase.autoStash", "true"),
        KeyValuePair.Create("submodule.recurse", "false"),
    ];

    private readonly TemporaryDirectory _temporaryDirectory = TemporaryDirectory.Create();
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly CancellationToken _cancellationToken;

    public FullPath Path => _temporaryDirectory.FullPath;

    private GitRepository(ITestOutputHelper testOutputHelper, CancellationToken cancellationToken)
    {
        _testOutputHelper = testOutputHelper;
        _cancellationToken = cancellationToken;
    }

    public static async Task<GitRepository> CreateAsync(ITestOutputHelper testOutputHelper, CancellationToken cancellationToken)
    {
        var repository = new GitRepository(testOutputHelper, cancellationToken);
        await repository.InitializeAsync();
        return repository;
    }

    private async Task InitializeAsync()
    {
        await ExecuteGitCommand(["init", "--initial-branch=main"]);
    }

    public ValueTask DisposeAsync() => _temporaryDirectory.DisposeAsync();

    public Task Commit(string? message = null, IEnumerable<(string FilePath, string Content)>? files = null)
    {
        return Commit(message, files?.Select(f => (f.FilePath, BinaryData.FromString(f.Content))));
    }

    public Task Commit(string? message = null, IEnumerable<(string FilePath, byte[] Content)>? files = null)
    {
        return Commit(message, files?.Select(f => (f.FilePath, BinaryData.FromBytes(f.Content))));
    }

    public async Task Commit(string? message = null, IEnumerable<(string FilePath, BinaryData Content)>? files = null)
    {
        if (files is not null)
        {
            foreach (var (filePath, content) in files)
            {
                var path = Path / filePath;
                path.CreateParentDirectory();
                await File.WriteAllBytesAsync(Path / filePath, content);

                await ExecuteGitCommand(["add", filePath]);
            }
        }

        await ExecuteGitCommand(["commit", "--message", message ?? "dummy"]);
    }

    public async Task Checkout(string @ref)
    {
        await ExecuteGitCommand(["checkout", @ref]);
        await ExecuteGitCommand(["clean", "-dfx"]);
    }

    public async Task<byte[]> GetFileContent(string @ref, string path)
    {
        var args = GetCommandArguments(["show", @ref + ":" + path]);
        using var outputStream = new MemoryStream();
        using var errorStream = new MemoryStream();
        var result = await ProcessWrapper.Create("git")
            .WithArguments(args)
            .WithOutputStream([outputStream])
            .WithErrorStream([errorStream])
            .WithValidation(ProcessValidationMode.None)
            .ExecuteAsync(_cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"git exited with code {result.ExitCode}: {Encoding.UTF8.GetString(errorStream.ToArray())}");
        }

        return outputStream.ToArray();
    }

    public async Task<string> GetFileContentAsString(string @ref, string path, Encoding? encoding = null)
    {
        var bytes = await GetFileContent(@ref, path);
        using var ms = new MemoryStream(bytes);
        using var sr = new StreamReader(ms, encoding ?? Encoding.UTF8);
        return sr.ReadToEnd();
    }

    private List<string> GetCommandArguments(IEnumerable<string?> args)
    {
        var result = new List<string>();
        result.AddRange("-C", Path.Value);
        foreach (var config in DefaultConfiguration)
        {
            result.Add("-c");
            result.Add($"{config.Key}={config.Value}");
        }

        foreach (var arg in args)
        {
            if (arg is not null)
            {
                result.Add(arg);
            }
        }

        return result;
    }

    private Task<int> ExecuteGitCommand(IEnumerable<string?> arguments)
        => ExecuteGitCommand(arguments, checkExitCode: true);

    private async Task<int> ExecuteGitCommand(IEnumerable<string?> arguments, bool checkExitCode)
    {
        var command = ProcessWrapper.Create("git")
            .WithArguments(GetCommandArguments(arguments))
            .AddOutputStream(_testOutputHelper.WriteLine)
            .AddErrorStream(_testOutputHelper.WriteLine);

        if (!checkExitCode)
        {
            command = command.WithValidation(ProcessValidationMode.None);
        }

        var result = await command.ExecuteAsync(_cancellationToken);

        return result.ExitCode;
    }
}
