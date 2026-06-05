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
            .ExcludeForks())
        .Build();

await new BatchProjectUpdater
{
    Logger = logger,
    Projects = projects,
    Updater = new AgentsFileUpdater(),
    OpenReviewUrlInBrowser = true,
    BatchOptions = new()
    {
        // MaximumProjects = 1,
    },
    ProjectUpdaterOptions = new()
    {
        ForcePush = true,
        BranchName = _ => new BranchName("instructions-file-migration"),
    },
}
.RunAsync();
