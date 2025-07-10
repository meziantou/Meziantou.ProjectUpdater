namespace Meziantou.ProjectUpdater;

public sealed record ChangelistInformation(string CommitId, string BranchName, Uri? ReviewUrl);