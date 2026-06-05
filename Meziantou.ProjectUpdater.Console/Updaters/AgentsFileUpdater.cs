using Meziantou.ProjectUpdater;

internal sealed class AgentsFileUpdater : IProjectUpdater
{
    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var file = context.LocalRepository.RootPath / ".github/copilot-instructions.md";
        if(!File.Exists(file))
            return null;

        File.Move(file, context.LocalRepository.RootPath / "AGENTS.md", overwrite: false);
        await File.WriteAllTextAsync(context.LocalRepository.RootPath / "CLAUDE.md", "@AGENTS.md");

        return new ChangeDescription("Use AGENTS.md");
    }
}
