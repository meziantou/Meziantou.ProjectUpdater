using System.Text;
using CliWrap;
using Meziantou.Framework;
using Meziantou.ProjectUpdater.GitHub;

namespace Meziantou.ProjectUpdater;

internal static class GitUtilities
{
    private static async Task<string> ExecuteGitCommand(FullPath path, ProjectUpdaterOptions options, string[] args, CancellationToken cancellationToken)
    {
        var arguments = new List<string>();
        foreach (var (key, value) in options.AdditionalGitConfigurations)
        {
            arguments.Add("-c");
            arguments.Add($"{key}={value}");
        }

        if (!path.IsEmpty)
        {
            arguments.Add("-C");
            arguments.Add(path);
        }

        arguments.AddRange(args);

        var logs = new StringBuilder();
        try
        {
            await Cli.Wrap("git")
                .WithArguments(arguments)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(logs))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(logs))
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var command = args.Length > 0 ? args[0] : string.Empty;
            throw new GitException($"git command '{command}' failed.\n" + logs, ex);
        }

        return logs.ToString();
    }

    public static Task CloneAsync(string remote, FullPath clonePath, ProjectUpdaterOptions options, CancellationToken cancellationToken = default)
    {
        return ExecuteGitCommand(FullPath.Empty, options, ["clone", remote, clonePath], cancellationToken);
    }

    public static async Task<string> CommitAsync(FullPath repositoryPath, ProjectUpdaterOptions options, string message, CancellationToken cancellationToken = default)
    {
        await ExecuteGitCommand(repositoryPath, options, ["add", "."], cancellationToken).ConfigureAwait(false);
        await ExecuteGitCommand(repositoryPath, options, ["commit", "-m", message], cancellationToken).ConfigureAwait(false);
        var commitId = await ExecuteGitCommand(repositoryPath, options, ["rev-parse", "HEAD"], cancellationToken).ConfigureAwait(false);
        return commitId.TrimEnd('\n');
    }

    public static Task PushAsync(FullPath repositoryPath, ProjectUpdaterOptions options, string branch, bool force, CancellationToken cancellationToken)
    {
        var additionalArgs = force ? ["--force"] : Array.Empty<string>();
        return ExecuteGitCommand(repositoryPath, options, ["push", "origin", "HEAD:" + branch, .. additionalArgs], cancellationToken);
    }

    public static async Task<string> GetCurrentBranchNameAsync(FullPath repositoryPath, ProjectUpdaterOptions options, CancellationToken cancellationToken)
    {
        var result = await ExecuteGitCommand(repositoryPath, options, ["rev-parse", "--abbrev-ref", "HEAD"], cancellationToken).ConfigureAwait(false);
        return result.TrimEnd('\n');
    }
}