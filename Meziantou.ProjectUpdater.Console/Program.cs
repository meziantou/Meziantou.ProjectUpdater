using Meziantou.ProjectUpdater;
using Meziantou.ProjectUpdater.Console.Updaters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var projects = new ProjectsCollectionBuilder()
        .AddGitHub(builder => builder
            .AddUserProjects("meziantou")
            //.AddOrganizationProjects("sample-org")
            .ExcludeArchivedProjects()
            .ExcludePrivateProjects()
            .ExcludeInternalProjects()
            .ExcludeForks())
        .Build();

await new BatchProjectUpdater
{
    Logger = logger,
    Projects = projects,
    Updater = new CreateCodeOwnersFileUpdater("meziantou"),
    OpenReviewUrlInBrowser = true,
    BatchOptions = new()
    {
        // MaximumProjects = 1,
    },
    ProjectUpdaterOptions = new()
    {
        ForcePush = true,
        //BranchName = _ => new BranchName("feature/"),
    },
}
.RunAsync();
