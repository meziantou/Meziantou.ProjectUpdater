using Meziantou.ProjectUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddConsole()).BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var projects = new ProjectsProviderBuilder()
        .AddGitHub(builder => builder
            .AddUserProjects("meziantou")
            .ExcludeArchivedProjects()
            .ExcludePrivateProjects()
            .ExcludeInternalProjects())
        .Build();

await new BatchProjectUpdater
{
    Logger = logger,
    Projects = projects,
    Updater = new CreateCodeOwnersFileUpdater("@meziantou"),
    OpenReviewUrlInBrowser = true,
}
.RunAsync();
