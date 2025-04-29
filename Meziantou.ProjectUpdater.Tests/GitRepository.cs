using System.Text;
using CliWrap;
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
        using var ms = new MemoryStream();
        var args = GetCommandArguments(["show", @ref + ":" + path]);
        var result = await Cli.Wrap("git")
           .WithArguments(args)
           .WithStandardOutputPipe(PipeTarget.ToStream(ms))
           .WithStandardErrorPipe(PipeTarget.ToStream(ms))
           .ExecuteAsync(_cancellationToken);

        return ms.ToArray();
    }

    public async Task<string> GetFileContentAsString(string @ref, string path, Encoding? encoding = null)
    {
        using var ms = new MemoryStream();
        var args = GetCommandArguments(["show", @ref + ":" + path]);
        var result = await Cli.Wrap("git")
           .WithArguments(args)
           .WithStandardOutputPipe(PipeTarget.ToStream(ms))
           .WithStandardErrorPipe(PipeTarget.ToStream(ms))
           .ExecuteAsync(_cancellationToken);

        ms.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(ms, encoding);
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
        var result = await Cli.Wrap("git")
            .WithArguments(GetCommandArguments(arguments))
            .WithStandardOutputPipe(PipeTarget.ToDelegate(_testOutputHelper.WriteLine))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(_testOutputHelper.WriteLine))
            .WithValidation(checkExitCode ? CommandResultValidation.ZeroExitCode : CommandResultValidation.None)
            .ExecuteAsync(_cancellationToken);

        return result.ExitCode;
    }
}