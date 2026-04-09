using System.Diagnostics;
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
        return await ExecuteGitCommandAndCaptureOutput(["show", @ref + ":" + path]).ConfigureAwait(false);
    }

    public async Task<string> GetFileContentAsString(string @ref, string path, Encoding? encoding = null)
    {
        var bytes = await ExecuteGitCommandAndCaptureOutput(["show", @ref + ":" + path]).ConfigureAwait(false);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
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
        var validationMode = checkExitCode ? ProcessValidationMode.FailIfNonZeroExitCode : ProcessValidationMode.None;
        using var process = ProcessWrapper.Create("git")
            .WithArguments(GetCommandArguments(arguments))
            .WithOutputStream(_testOutputHelper.WriteLine)
            .AddErrorStream(_testOutputHelper.WriteLine)
            .WithValidation(validationMode)
            .ExecuteAsync(_cancellationToken);

        return await process;
    }

    private async Task<byte[]> ExecuteGitCommandAndCaptureOutput(IEnumerable<string?> arguments)
    {
        using var standardOutput = new MemoryStream();
        using var standardError = new MemoryStream();

        var processStartInfo = new ProcessStartInfo("git")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in GetCommandArguments(arguments))
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = processStartInfo,
        };

        process.Start();

        var copyStandardOutputTask = process.StandardOutput.BaseStream.CopyToAsync(standardOutput, _cancellationToken);
        var copyStandardErrorTask = process.StandardError.BaseStream.CopyToAsync(standardError, _cancellationToken);
        var waitForExitTask = process.WaitForExitAsync(_cancellationToken);

        await Task.WhenAll(copyStandardOutputTask, copyStandardErrorTask, waitForExitTask).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = Encoding.UTF8.GetString(standardError.ToArray());
            throw new InvalidOperationException($"git command failed with exit code {process.ExitCode}: {stderr}");
        }

        var result = new byte[standardOutput.Length + standardError.Length];
        Buffer.BlockCopy(standardOutput.GetBuffer(), 0, result, 0, (int)standardOutput.Length);
        Buffer.BlockCopy(standardError.GetBuffer(), 0, result, (int)standardOutput.Length, (int)standardError.Length);
        return result;
    }
}
