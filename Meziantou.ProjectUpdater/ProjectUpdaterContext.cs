using Microsoft.Extensions.Logging;

namespace Meziantou.ProjectUpdater;

public sealed class ProjectUpdaterContext
{
    internal ProjectUpdaterContext(ILogger logger, LocalRepository localRepository, Project project, ProjectUpdaterOptions options, CancellationToken cancellationToken)
    {
        Logger = logger;
        LocalRepository = localRepository;
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Options = options;
        CancellationToken = cancellationToken;
    }

    public CancellationToken CancellationToken { get; }
    public ILogger Logger { get; }
    public LocalRepository LocalRepository { get; }
    public Project Project { get; }
    public ProjectUpdaterOptions Options { get; }
}
