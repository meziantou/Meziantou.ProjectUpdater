namespace Meziantou.ProjectUpdater.Tests;

public class CreateCodeOwnersFileUpdaterTests
{
    [Fact]
    public async Task Test1()
    {
        await using var context = new ProjectUpdaterTestContext()
        {
            ProjectUpdater = new CreateCodeOwnersFileUpdater("@test"),
        };

        var repo = await context.CreateGitRepository();
        await repo.Commit("dummy", [("README.md", "dummy")]);
        await context.RunAsync();

        Assert.Equal("* @test\n", await repo.GetFileContentAsString("add-codeowners-file", "CODEOWNERS"));
    }
}
