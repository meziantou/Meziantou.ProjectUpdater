#pragma warning disable CA2000 // Dispose objects before losing scope
using Meziantou.ProjectUpdater.GitHub.Client.Internals;

namespace Meziantou.ProjectUpdater.GitHub.Client;

internal sealed partial class GitHubClient
{
    public async Task<GitHubRepository?> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"repos/{owner}/{repo}");
        return await GetAsync<GitHubRepository>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPageResponse<GitHubMinimalRepository>> GetRepositories(string @namespace, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetUserRepositories(@namespace, cancellationToken).ConfigureAwait(false);
        }
        catch (GitHubException ex) when (ex.HttpStatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return await GetOrganizationRepositories(@namespace, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<GitHubPageResponse<GitHubMinimalRepository>> GetUserRepositories(string username, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"users/{username}/repos?per_page=100");
        return await GetPagedCollectionAsync<GitHubMinimalRepository>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPageResponse<GitHubMinimalRepository>> GetOrganizationRepositories(string organization, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"orgs/{organization}/repos?per_page=100");
        return await GetPagedCollectionAsync<GitHubMinimalRepository>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPageResponse<GitHubMinimalRepository>> GetStarredRepositories(string username, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"users/{username}/starred?per_page=100");
        return await GetPagedCollectionAsync<GitHubMinimalRepository>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPageResponse<GitHubMinimalRepository>> GetCurrentUserRepositories(CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"user/repos?per_page=100");
        return await GetPagedCollectionAsync<GitHubMinimalRepository>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPageResponse<GitHubPullRequest>> GetPullRequests(string owner, string repo, string? head, string? @base, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"/repos/{owner}/{repo}/pulls?per_page=100");
        url.AppendQuery("state", "open");
        if (head is not null)
        {
            url.AppendQuery("head", head);
        }
        
        if (@base is not null)
        {
            url.AppendQuery("base", @base);
        }

        return await GetPagedCollectionAsync<GitHubPullRequest>(url.ToString(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitHubPullRequest> CreatePullRequestAsync(string owner, string repo, CreateGitHubPullRequest request, CancellationToken cancellationToken = default)
    {
        var url = new UrlBuilder($"repos/{owner}/{repo}/pulls");
        return await PostAsync<CreateGitHubPullRequest, GitHubPullRequest>(url.ToString(), request, cancellationToken).ConfigureAwait(false);
    }
}
