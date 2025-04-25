namespace Meziantou.ProjectUpdater;

public abstract class ProjectUpdaterRateLimiter
{
    public abstract ValueTask WaitAsync(CancellationToken cancellationToken = default);
    public abstract void UpdateCompleted(ChangelistInformation? pushResult);

    public static ProjectUpdaterRateLimiter NoLimit => NoRateLimiter.Instance;

    private sealed class NoRateLimiter : ProjectUpdaterRateLimiter
    {
        public static NoRateLimiter Instance { get; } = new NoRateLimiter();

        public override void UpdateCompleted(ChangelistInformation? pushResult) { }
        public override ValueTask WaitAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}