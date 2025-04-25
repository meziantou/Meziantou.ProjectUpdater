using System.Globalization;
using Meziantou.ProjectUpdater.GitHub.Client;
using Meziantou.Framework;

namespace Meziantou.ProjectUpdater.GitHub;
internal sealed class GitHubProject : Project
{
    private readonly GitHubClient _client;

    public GitHubProject(GitHubClient client, GitHubMinimalRepository repo)
        : this(client, repo.Id, repo.Owner?.Login!, repo.Name!, repo.CloneUrl!, repo.Archived, repo.Visibility!)
    {
    }

    public GitHubProject(GitHubClient client, long id, string owner, string name, string cloneUrl, bool isArchived, string visibility)
        : base(string.Create(CultureInfo.InvariantCulture, $"github:{id}"), $"{owner}/{name}")
    {
        ArgumentNullException.ThrowIfNull(cloneUrl);
        ArgumentNullException.ThrowIfNull(visibility);

        _client = client;
     
        RepoOwner = owner;
        RepoName = name;
        CloneUrl = cloneUrl;
        IsArchived = isArchived;
        Visibility = visibility;
    }

    public string RepoOwner { get; }
    public string RepoName { get; }
    public string CloneUrl { get; }
    public bool IsArchived { get; }
    public string Visibility { get; }

    public string FullName => $"{RepoOwner}/{RepoName}";

    public override Task CloneAsync(FullPath clonePath, ProjectUpdaterOptions options, CancellationToken cancellationToken = default)
    {
        if (CloneUrl is null)
            throw new InvalidOperationException("Repository doesn't have a clone url");

        return GitUtilities.CloneAsync(CloneUrl, clonePath, options, cancellationToken);
    }

    public override async Task<ChangelistInformation> CreateChangelistAsync(CreateChangelistRequest request)
    {
        var currentBranchName = new BranchName(await GitUtilities.GetCurrentBranchNameAsync(request.Repository.RootPath, request.Options, request.CancellationToken).ConfigureAwait(false));
        var branchName = request.ChangeDescription.BranchName ?? request.Options.BranchName?.Invoke(request) ?? currentBranchName;

        var message = request.ChangeDescription.Title;
        if (!string.IsNullOrEmpty(request.ChangeDescription.Description))
            message = message + "\n\n" + request.ChangeDescription.Description;

        var commitId = await GitUtilities.CommitAsync(request.Repository.RootPath, request.Options, message, request.CancellationToken).ConfigureAwait(false);
        await GitUtilities.PushAsync(request.Repository.RootPath, request.Options, branch: branchName.Name, force: request.Options.ForcePush, cancellationToken: request.CancellationToken).ConfigureAwait(false);

        Uri? pullRequestUrl = null;
        if (currentBranchName != branchName)
        {
            try
            {
                var pullRequest = await _client.CreatePullRequestAsync(RepoOwner, RepoName, new CreateGitHubPullRequest
                {
                    Title = request.ChangeDescription.Title,
                    Body = request.ChangeDescription.Description,
                    Head = branchName.Name,
                    Base = currentBranchName.Name,
                }, request.CancellationToken).ConfigureAwait(false);

                pullRequestUrl = pullRequest.HtmlUrl;
            }
            catch (GitHubException)
            {
                // Check if a PR is already opened for the branch
                await foreach (var pullRequest in (await _client.GetPullRequests(RepoOwner, RepoName, head: branchName.Name, @base: currentBranchName.Name, request.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false).WithCancellation(request.CancellationToken))
                {
                    if (pullRequest.HtmlUrl is not null)
                    {
                        pullRequestUrl = pullRequest.HtmlUrl;
                        break;
                    }
                }
            }
        }

        var result = new ChangelistInformation(commitId, pullRequestUrl);
        return result;
    }
}
