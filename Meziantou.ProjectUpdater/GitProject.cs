using Meziantou.Framework;

namespace Meziantou.ProjectUpdater;

internal abstract class GitProject : Project
{
    protected GitProject(string id, string name)
        : base(id, name)
    {
    }

    public abstract string CloneUrl { get; }

    public override Task CloneAsync(FullPath clonePath, ProjectUpdaterOptions options, CancellationToken cancellationToken = default)
    {
        if (CloneUrl is null)
            throw new InvalidOperationException("Repository doesn't have a clone url");

        return GitUtilities.CloneAsync(CloneUrl, clonePath, options, cancellationToken);
    }

    public override async Task<ChangelistInformation> CreateChangelistAsync(CreateChangelistRequest request)
    {
        var currentBranchName = new BranchName(await GitUtilities.GetCurrentBranchNameAsync(request.Repository.RootPath, request.Options, request.CancellationToken).ConfigureAwait(false));
        var branchName = request.ChangeDescription.BranchName ?? request.Options.BranchName?.Invoke(request) ?? currentBranchName;

        var message = request.ChangeDescription.Title;
        if (!string.IsNullOrEmpty(request.ChangeDescription.Description))
            message = message + "\n\n" + request.ChangeDescription.Description;

        var commitId = await GitUtilities.CommitAsync(request.Repository.RootPath, request.Options, message, request.CancellationToken).ConfigureAwait(false);
        await GitUtilities.PushAsync(request.Repository.RootPath, request.Options, branch: branchName.Name, force: request.Options.ForcePush, cancellationToken: request.CancellationToken).ConfigureAwait(false);

        Uri? pullRequestUrl = null;
        if (currentBranchName != branchName)
            pullRequestUrl = await CreatePullRequestAsync(request, currentBranchName, branchName, request.CancellationToken).ConfigureAwait(false);

        var result = new ChangelistInformation(commitId, pullRequestUrl);
        return result;

    }

    protected abstract Task<Uri?> CreatePullRequestAsync(CreateChangelistRequest request, BranchName sourceBranchName, BranchName targetBranchName, CancellationToken cancellationToken);
}
