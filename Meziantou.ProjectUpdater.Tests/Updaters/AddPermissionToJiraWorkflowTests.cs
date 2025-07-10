using Meziantou.ProjectUpdater.Console.Updaters;

namespace Meziantou.ProjectUpdater.Tests;

public class AddPermissionToJiraWorkflowTests
{
    [Fact]
    public async Task Test1()
    {
        await using var context = new ProjectUpdaterTestContext()
        {
            ProjectUpdater = new AddPermissionToJiraWorkflow(),
        };

        var repo = await context.CreateGitRepository();
        await repo.Commit("dummy", [(".github/workflows/jira.yml", """
            name: Jira

            on:
              pull_request:
                branches: [main]
                paths-ignore: ["*.md"]

            jobs:
              call-workflow-jira:
                uses: workleap/wl-reusable-workflows/.github/workflows/reusable-jira-workflow.yml@main
                with:
                  branch_name: ${{ github.head_ref }}
                permissions:
                  contents: read
                  id-token: write
            """)]);
        await context.RunAsync();

        string actualFileContent = await repo.GetFileContentAsString("add-pull-requests-write-permissions-to-the-jira-job", ".github/workflows/jira.yml");
        Assert.Equal("""
            name: Jira
            
            on:
              pull_request:
                branches: [main]
                paths-ignore: ["*.md"]
            
            jobs:
              call-workflow-jira:
                uses: workleap/wl-reusable-workflows/.github/workflows/reusable-jira-workflow.yml@main
                permissions:
                  contents: read
                  pull-requests: write
                  id-token: write
            """, actualFileContent);
    }
}
