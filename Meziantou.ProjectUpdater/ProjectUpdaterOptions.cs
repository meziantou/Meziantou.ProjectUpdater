namespace Meziantou.ProjectUpdater;

public sealed class ProjectUpdaterOptions
{
    public bool ForcePush { get; set; }
    public Func<CreateChangelistRequest, BranchName?> BranchName { get; set; } = request => Meziantou.ProjectUpdater.BranchName.Slugify(request.ChangeDescription.Title);
    public IList<(string Key, string Value)> AdditionalGitConfigurations { get; } = [];
}