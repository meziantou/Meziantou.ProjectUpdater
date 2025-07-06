using CliWrap;
using Meziantou.ProjectUpdater;

internal sealed class SlnxUpdater : IProjectUpdater
{
    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var repo = context.LocalRepository;
        var count = 0;
        foreach (var slnPath in repo.FindFile("**/*.sln"))
        {
            await Cli.Wrap("dotnet")
                .WithArguments(["sln", slnPath, "migrate"])
                .ExecuteAsync();

            File.Delete(slnPath); // Delete the old .sln file

            // Update all references to .sln
            foreach (var file in repo.FindFile("**/*"))
            {
                await repo.UpdateFileAsync(file, text =>
                {
                    var oldText = Path.GetFileName(slnPath);
                    var newText = oldText + "x";
                    text = text.Replace(oldText, newText, StringComparison.OrdinalIgnoreCase);
                    return text;
                });
            }

            count++;
        }

        if (count == 0)
            return null;

        return new ChangeDescription("Convert sln to slnx");
    }
}
