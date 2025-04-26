namespace Meziantou.ProjectUpdater;

public interface IProjectUpdater
{
    bool CloneProjectAutomatically => true;

    ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context);
}