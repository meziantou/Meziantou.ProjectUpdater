using System.Text.RegularExpressions;
using Meziantou.ProjectUpdater;

internal sealed class RemoveSelfHostedGitHubActionsRunsOnFileUpdater() : IProjectUpdater
{
    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var updated = false;
        var repo = context.LocalRepository;
        var extensions = new[] { ".yml", ".yaml" };
        var files = extensions.SelectMany(ext => Directory.GetFiles(repo.RootPath / ".github" / "workflows", $"*{ext}", SearchOption.AllDirectories));
        foreach (var file in files)
        {
            updated |= await repo.UpdateFileAsync(file, content =>
            {
                return Regex.Replace(content, @"(?<=runs-on:\s*\[\s*)self-hosted\s*,\s*", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            });
        }

        return updated ? new ChangeDescription("Update self-hosted runners") : null;
    }
}
