namespace Meziantou.ProjectUpdater;

public interface IProjectUpdater
{
    ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context);
}