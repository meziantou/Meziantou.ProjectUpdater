using Meziantou.Framework;

namespace Meziantou.ProjectUpdater;
internal sealed class LocalGitProject(FullPath path) : GitProject("local:" + path, Path.GetFileName(path))
{
    public override string CloneUrl { get; } = path;

    protected override Task<Uri?> CreatePullRequestAsync(CreateChangelistRequest request, BranchName sourceBranchName, BranchName targetBranchName, CancellationToken cancellationToken)
    {
        return Task.FromResult<Uri?>(null);
    }
}
