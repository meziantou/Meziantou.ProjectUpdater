using Meziantou.Framework;
using Meziantou.ProjectUpdater.AzureDevOps;
using Meziantou.ProjectUpdater.GitHub;

namespace Meziantou.ProjectUpdater;

public sealed class ProjectsCollectionBuilder
{
    private readonly List<Func<IAsyncEnumerable<Project>>> _providers = [];

    public ProjectsCollectionBuilder AddGitHub(Action<GitHubProjectsProviderBuilder> builder)
    {
        var provider = new GitHubProjectsProviderBuilder();
        builder?.Invoke(provider);
        var result = provider.Build();
        _providers.Add(result);
        return this;
    }

    public ProjectsCollectionBuilder AddAzureDevOps(Action<AzureDevOpsProjectsProviderBuilder> builder)
    {
        var provider = new AzureDevOpsProjectsProviderBuilder();
        builder?.Invoke(provider);
        var result = provider.Build();
        _providers.Add(result);
        return this;
    }

    public ProjectsCollectionBuilder AddLocalGitRepository(FullPath path)
    {
        _providers.Add(() => new Project[] { new LocalGitProject(path) }.ToAsyncEnumerable());
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