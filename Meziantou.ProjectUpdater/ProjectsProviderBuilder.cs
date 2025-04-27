using Meziantou.ProjectUpdater.AzureDevOps;
using Meziantou.ProjectUpdater.GitHub;

namespace Meziantou.ProjectUpdater;

public sealed class ProjectsProviderBuilder
{
    private readonly List<Func<IAsyncEnumerable<Project>>> _providers = [];

    public ProjectsProviderBuilder AddGitHub(Action<GitHubProjectsProviderBuilder> builder)
    {
        var provider = new GitHubProjectsProviderBuilder();
        builder?.Invoke(provider);
        var result = provider.Build();
        _providers.Add(result);
        return this;
    }
  
    public ProjectsProviderBuilder AddAzureDevOps(Action<AzureDevOpsProjectsProviderBuilder> builder)
    {
        var provider = new AzureDevOpsProjectsProviderBuilder();
        builder?.Invoke(provider);
        var result = provider.Build();
        _providers.Add(result);
        return this;
    }

    public async IAsyncEnumerable<Project> Build()
    {
        foreach (var provider in _providers)
        {
            await foreach (var project in provider().ConfigureAwait(false))
            {
                yield return project;
            }
        }
    }
}