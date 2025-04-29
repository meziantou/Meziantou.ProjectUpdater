using System.Globalization;
using Meziantou.ProjectUpdater.GitHub.Client;

namespace Meziantou.ProjectUpdater.GitHub;

internal sealed class GitHubProject : GitProject
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
    public override string CloneUrl { get; }
    public bool IsArchived { get; }
    public string Visibility { get; }

    public string FullName => $"{RepoOwner}/{RepoName}";

    protected override async Task<Uri?> CreatePullRequestAsync(CreateChangelistRequest request, BranchName sourceBranchName, BranchName targetBranchName, CancellationToken cancellationToken)
    {
        try
        {
            var pullRequest = await _client.CreatePullRequestAsync(RepoOwner, RepoName, new CreateGitHubPullRequest
            {
                Title = request.ChangeDescription.Title,
                Body = request.ChangeDescription.Description,
                Head = sourceBranchName.Name,
                Base = targetBranchName.Name,
            }, request.CancellationToken).ConfigureAwait(false);

            return pullRequest.HtmlUrl;
        }
        catch (GitHubException)
        {
            // Check if a PR is already opened for the branch
            await foreach (var pullRequest in (await _client.GetPullRequests(RepoOwner, RepoName, head: sourceBranchName.Name, @base: targetBranchName.Name, request.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false).WithCancellation(request.CancellationToken))
            {
                if (pullRequest.HtmlUrl is not null)
                {
                    return pullRequest.HtmlUrl;
                }
            }
        }

        return null;
    }
}
