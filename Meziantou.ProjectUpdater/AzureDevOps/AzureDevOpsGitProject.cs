using System.Globalization;
using Meziantou.Framework;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Meziantou.ProjectUpdater.AzureDevOps;

internal sealed class AzureDevOpsGitProject : GitProject
{
    private readonly GitHttpClient _client;
    private readonly string? _personalAccessToken;
    private readonly Guid _repositoryId;
    private readonly Guid _projectId;

    public AzureDevOpsGitProject(GitHttpClient client, GitRepository repository, string? personalAccessToken)
        : base(string.Create(CultureInfo.InvariantCulture, $"AzureDevOps:{repository.Id}"), repository.Name)
    {
        _repositoryId = repository.Id;
        _projectId = repository.ProjectReference.Id;
        _client = client;
        _personalAccessToken = personalAccessToken;
        IsDisabled = repository.IsDisabled ?? false;
        IsFork = repository.IsFork;
        if (personalAccessToken is null)
        {
            CloneUrl = repository.RemoteUrl;
        }
        else
        {
            CloneUrl = new UriBuilder(repository.RemoteUrl) { Password = personalAccessToken }.Uri.ToString();
        }
    }

    public bool IsDisabled { get; }
    public bool IsFork { get; set; }
    public override string CloneUrl { get; }

    public override async Task CloneAsync(FullPath clonePath, ProjectUpdaterOptions options, CancellationToken cancellationToken = default)
    {
        await base.CloneAsync(clonePath, options, cancellationToken).ConfigureAwait(false);
        if (_personalAccessToken is not null)
        {
            var headerValue = "Authorization: Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(":" + _personalAccessToken));
            await GitUtilities.ExecuteGitCommand(clonePath, options, ["config", "http.extraheader", headerValue], cancellationToken).ConfigureAwait(false);
        }
    }

    protected override async Task<Uri?> CreatePullRequestAsync(CreateChangelistRequest request, BranchName sourceBranch, BranchName targetBranch, CancellationToken cancellationToken)
    {
        var sourceBranchName = "refs/heads/" + sourceBranch.Name;
        var targetBranchName = "refs/heads/" + targetBranch.Name;

        try
        {
            var pr = await _client.CreatePullRequestAsync(
                new GitPullRequest()
                {
                    SourceRefName = targetBranchName,
                    TargetRefName = sourceBranchName,
                    Title = request.ChangeDescription.Title,
                    Description = request.ChangeDescription.Description,
                },
                repositoryId: _repositoryId,
                project: _projectId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return CreateUrl(pr);
        }
        catch
        {
            var existingPullRequests = await _client.GetPullRequestsAsync(_repositoryId, new() { SourceRefName = targetBranchName, TargetRefName = sourceBranchName, Status = PullRequestStatus.Active }, maxCommentLength: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (existingPullRequests.Count > 0)
                return CreateUrl(existingPullRequests[0]);
        }

        return null;

        static Uri CreateUrl(GitPullRequest pr)
        {
            // Convert the API URL to a web URL
            // https://dev.azure.com/{CollectionName}/{ProjectGuid}/_apis/git/repositories/{RepositoryGuid}/pullRequests/{PullRequestId}
            // https://dev.azure.com/{CollectionName}/{ProjectName}/_git/{RepositoryName}/pullrequest/{PullRequestId}
            var projectName = pr.Repository.ProjectReference.Name;
            var repositoryName = pr.Repository.Name;
            var textToReplace = $"{pr.Repository.ProjectReference.Id}/_apis/git/repositories/{pr.Repository.Id}/pullRequests/";
            var newValue = $"{Uri.EscapeDataString(projectName)}/_git/{Uri.EscapeDataString(repositoryName)}/pullrequest/";
            var newUri = pr.Url.Replace(textToReplace, newValue, StringComparison.Ordinal);
            return new Uri(newUri);
        }
    }
}
