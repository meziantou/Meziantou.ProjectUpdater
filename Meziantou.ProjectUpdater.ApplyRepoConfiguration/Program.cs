using Meziantou.ProjectUpdater;
using Meziantou.ProjectUpdater.Console.Updaters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var projects = new ProjectsCollectionBuilder()
        .AddGitHub(builder => builder
            .AddUserProjects("meziantou")
            .ExcludeArchivedProjects()
            .ExcludePrivateProjects()
            .ExcludeInternalProjects()
            .ExcludeForks())
        .Build();

await new BatchProjectUpdater
{
    Logger = logger,
    Projects = projects,
    Updater = new ApplyRepositoryConfiguration(),
    OpenReviewUrlInBrowser = true,
    BatchOptions = new()
    {
        DegreeOfParallelism = 4,
    },
    ProjectUpdaterOptions = new()
    {
        ForcePush = true,
    },
}
.RunAsync();
