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
            .ExcludeInternalProjects())
        .AddAzureDevOps(builder => builder
            .SetCollection(new("https://dev.azure.com/meziantou"))
            .Authenticate("pat") // needs at least code:read-write
            .AddAccessibleRepositories()
            .ExcludeDisabledRepositories()
            .ExcludeForks())
        .Build();

await new BatchProjectUpdater
{
    Logger = logger,
    Projects = projects,
    Updater = new CreateCodeOwnersFileUpdater("@meziantou"),
    OpenReviewUrlInBrowser = true,
    ProjectUpdaterOptions = new()
    {
        ForcePush = true,
        //BranchName = _ => new BranchName("feature/"),
    },
}
.RunAsync();
