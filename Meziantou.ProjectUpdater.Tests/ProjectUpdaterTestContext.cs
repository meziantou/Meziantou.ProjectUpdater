using Meziantou.BatchUpdates;
using Meziantou.Extensions.Logging.Xunit.v3;
using Meziantou.Framework;

namespace Meziantou.ProjectUpdater.Tests;

internal sealed class ProjectUpdaterTestContext : IAsyncDisposable
{
    private readonly TemporaryDirectory _tempDirectory;

    public ProjectUpdaterTestContext()
    {
        _tempDirectory = TemporaryDirectory.Create();
        ProgressionFilePath = _tempDirectory.FullPath / "progression.json";
    }

    public IProjectUpdater? ProjectUpdater { get; set; }
    public GitRepository? Repository { get; set; }
    private FullPath ProgressionFilePath { get; }

    public async Task<GitRepository> CreateGitRepository()
    {
        if (Repository is not null)
            throw new InvalidOperationException("Git repository already created.");

        Repository = await GitRepository.CreateAsync(TestContext.Current.TestOutputHelper!, TestContext.Current.CancellationToken);
        return Repository;
    }

    public async Task RunAsync()
    {
        if (ProjectUpdater is null)
            throw new InvalidOperationException("ProjectUpdater is not initialized.");

        if (Repository is null)
            throw new InvalidOperationException("Repository is not initialized.");

        var projectOptions = new ProjectUpdaterOptions();
        foreach (var kvp in GitRepository.DefaultConfiguration.Select(kvp => (kvp.Key, kvp.Value)))
        {
            projectOptions.AdditionalGitConfigurations.Add(kvp);
        }

        var updater = new BatchProjectUpdater()
        {
            Logger = XUnitLogger.CreateLogger(),
            OpenReviewUrlInBrowser = false,
            Updater = ProjectUpdater,
            DatabasePath = ProgressionFilePath,
            Projects = new ProjectsCollectionBuilder().AddLocalGitRepository(Repository.Path).Build(),
            ProjectUpdaterOptions = projectOptions,
        };

        await updater.RunAsync(TestContext.Current.CancellationToken);

        var db = await Database.Load(ProgressionFilePath, TestContext.Current.CancellationToken);
        var errors = await db.GetAllErrors();
        Assert.Empty(errors);
    }

    public async ValueTask DisposeAsync()
    {
        await _tempDirectory.DisposeAsync();
    }
}