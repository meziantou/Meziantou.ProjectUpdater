using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using Meziantou.ProjectUpdater.GitHub.Client;

namespace Meziantou.ProjectUpdater.GitHub;

public sealed class GitHubProjectsProviderBuilder
{
    private string? _token;
    private readonly List<Func<GitHubClient, CancellationToken, IAsyncEnumerable<Project>>> _providers = [];
    private readonly List<Func<IAsyncEnumerable<Project>, IAsyncEnumerable<Project>>> _filters = [];
    private readonly HashSet<string> _excludedVisibilities = [];
    private bool _includeArchived = true;

    public GitHubProjectsProviderBuilder Authenticate(string? token)
    {
        _token = token;
        return this;
    }

    public GitHubProjectsProviderBuilder AddUserProjects(string name)
    {
        _providers.Add(Convert((client, cancellationToken) => client.GetUserRepositories(name, cancellationToken)));
        return this;
    }

    public GitHubProjectsProviderBuilder AddOrganizationProjects(string name)
    {
        _providers.Add(Convert((client, cancellationToken) => client.GetOrganizationRepositories(name, cancellationToken)));
        return this;
    }

    public GitHubProjectsProviderBuilder AddProject(string owner, string repo)
    {
        _providers.Add(Convert((client, cancellationToken) => client.GetRepositoryAsync(owner, repo, cancellationToken)));
        return this;
    }

    public GitHubProjectsProviderBuilder ExcludeProject(string owner, string repo)
    {
        _filters.Add(source => source.Where(p =>
        {
            var project = (GitHubProject)p;
            return project.RepoOwner != owner || project.RepoName != repo;
        }));

        return this;
    }

    public GitHubProjectsProviderBuilder ExcludeProjectsFrom(string owner)
    {
        _filters.Add(source => source.Where(p =>
        {
            var project = (GitHubProject)p;
            return project.RepoOwner != owner;
        }));

        return this;
    }

    private GitHubProjectsProviderBuilder ExcludeRepositoryVisibility(string visibility)
    {
        _excludedVisibilities.Add(visibility);
        return this;
    }

    public GitHubProjectsProviderBuilder ExcludePrivateProjects() => ExcludeRepositoryVisibility("private");
    public GitHubProjectsProviderBuilder ExcludePublicProjects() => ExcludeRepositoryVisibility("public");
    public GitHubProjectsProviderBuilder ExcludeInternalProjects() => ExcludeRepositoryVisibility("internal");

    public GitHubProjectsProviderBuilder ExcludeArchivedProjects()
    {
        _includeArchived = false;
        return this;
    }

    internal Func<IAsyncEnumerable<Project>> Build()
    {
        return () =>
        {
            var result = CombineProjects();

            if (!_includeArchived)
                result = result.Where(p => !((GitHubProject)p).IsArchived);

            if (_excludedVisibilities.Count > 0)
                result = result.Where(p => !_excludedVisibilities.Contains(((GitHubProject)p).Visibility));

            foreach (var filter in _filters)
            {
                result = filter(result);
            }

            return result;
        };

        async IAsyncEnumerable<Project> CombineProjects([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = await CreateClient(cancellationToken).ConfigureAwait(false);
            foreach (var provider in _providers)
            {
                await foreach (var project in provider(client, cancellationToken).ConfigureAwait(false))
                {
                    yield return project;
                }
            }
        }
    }

    private static Func<GitHubClient, CancellationToken, IAsyncEnumerable<Project>> Convert(Func<GitHubClient, CancellationToken, Task<GitHubPageResponse<GitHubMinimalRepository>>> func)
    {
        return ConvertCore;

        async IAsyncEnumerable<Project> ConvertCore(GitHubClient client, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var response = await func(client, cancellationToken).ConfigureAwait(false);
            await foreach (var item in response.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                yield return new GitHubProject(client, item);
            }
        }
    }

    private static Func<GitHubClient, CancellationToken, IAsyncEnumerable<Project>> Convert(Func<GitHubClient, CancellationToken, Task<GitHubRepository?>> func)
    {
        return ConvertCore;

        async IAsyncEnumerable<Project> ConvertCore(GitHubClient client, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var item = await func(client, cancellationToken).ConfigureAwait(false);
            if (item is not null)
                yield return new GitHubProject(client, item);
        }
    }

    private async ValueTask<GitHubClient> CreateClient(CancellationToken cancellationToken)
    {
        var token = _token;
        token ??= Environment.GetEnvironmentVariable("GH_TOKEN");
        if (token is null)
        {
            try
            {
                var sb = new StringBuilder();
                await Cli.Wrap("gh")
                    .WithArguments(["auth", "token"])
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb))
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                token = sb.ToString().Trim(' ', '\r', '\n');
            }
            catch
            {
            }
        }

        return GitHubClient.Create(GitHubClient.GitHubPublicUri, token);
    }
}