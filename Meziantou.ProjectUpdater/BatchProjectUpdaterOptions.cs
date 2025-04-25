namespace Meziantou.ProjectUpdater;

public sealed class BatchProjectUpdaterOptions
{
    /// <summary>
    /// Throttle the update process to avoid overloading the server. For instance, you can wait for the CI to finish before starting the next update.
    /// </summary>
    public ProjectUpdaterRateLimiter? RateLimiter { get; set; }

    /// <summary>
    /// Set the maximum number of projects to update. If <see langword="null"/>, all projects will be updated.
    /// </summary>
    public int? MaximumProjects { get; set; }

    /// <summary>
    /// Set the maximum number of concurrent updates.
    /// </summary>
    public int DegreeOfParallelism { get; set; } = 1;
}