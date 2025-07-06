using Meziantou.ProjectUpdater;

internal sealed class CreateCodeOwnersFileUpdater(string owners) : IProjectUpdater
{
    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var repo = context.LocalRepository;
        if (File.Exists(repo.RootPath / "CODEOWNERS"))
            return null;

        if (File.Exists(repo.RootPath / ".github" / "CODEOWNERS"))
            return null;

        if (File.Exists(repo.RootPath / "docs" / "CODEOWNERS"))
            return null;

        await repo.AddFileAsync("CODEOWNERS", $"* {owners}\n");
        return new ChangeDescription("Add CODEOWNERS file", "");
    }
}
