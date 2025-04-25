namespace Meziantou.ProjectUpdater;

public sealed class CreateChangelistRequest
{
    internal CreateChangelistRequest(LocalRepository repository, ProjectUpdaterOptions options, ChangeDescription changeDescription, CancellationToken cancellationToken)
    {
        Repository = repository;
        Options = options;
        ChangeDescription = changeDescription;
        CancellationToken = cancellationToken;
    }

    public LocalRepository Repository { get; }
    public ProjectUpdaterOptions Options { get; }
    public ChangeDescription ChangeDescription { get; }
    public CancellationToken CancellationToken { get; }
}
