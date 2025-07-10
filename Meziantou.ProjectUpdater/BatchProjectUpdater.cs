using System.Diagnostics;
using Meziantou.BatchUpdates;
using Meziantou.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Meziantou.ProjectUpdater;

public sealed partial class BatchProjectUpdater
{
    public IAsyncEnumerable<Project>? Projects { get; set; }
    public BatchProjectUpdaterOptions? BatchOptions { get; set; }
    public ProjectUpdaterOptions? ProjectUpdaterOptions { get; set; }
    public IProjectUpdater? Updater { get; set; }
    public ILogger? Logger { get; set; }

    /// <summary>
    /// The database is used to store the progress of the updates. 
    /// It can restore the progress in case of failure.
    /// </summary>
    public FullPath DatabasePath { get; set; }

    /// <summary>
    /// Set to <see langword="true" /> to open the changelist URL in a local browser.
    /// </summary>
    public bool OpenReviewUrlInBrowser { get; set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (Projects is null)
            throw new InvalidOperationException("Projects is not set.");
        if (Updater is null)
            throw new InvalidOperationException("Updater is not set.");

        var logger = Logger ?? NullLogger.Instance;
        var batchOptions = BatchOptions ?? new BatchProjectUpdaterOptions();
        using var database = await Database.Load(DatabasePath, cancellationToken).ConfigureAwait(false);
        var rateLimiter = batchOptions.RateLimiter ?? ProjectUpdaterRateLimiter.NoLimit;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = batchOptions.DegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(Projects.Distinct().Take(batchOptions.MaximumProjects ?? int.MaxValue), parallelOptions, async (project, cancellationToken) =>
        {
            Log.ProcessingProject(logger, project.Id, project.Name);

            await database.AddProject(project).ConfigureAwait(false);
            if (await database.IsProcessed(project).ConfigureAwait(false))
            {
                Log.ProjectAlreadyProcessed(logger, project.Id, project.Name);
                return;
            }

            await rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            var options = ProjectUpdaterOptions ?? new ProjectUpdaterOptions();

            var temporaryDirectory = TemporaryDirectory.Create();
            try
            {
                var localRepository = new LocalRepository(logger, temporaryDirectory, project, options, cancellationToken);
                if (Updater.CloneProjectAutomatically)
                {
                    Log.CloningProject(logger, project.Id, project.Name);
                    await project.CloneAsync(temporaryDirectory.FullPath, options, cancellationToken).ConfigureAwait(false);
                }

                var context = new ProjectUpdaterContext(logger, localRepository, project, options, cancellationToken);

                Log.UpdatingProject(logger, project.Id, project.Name);
                var changeDescription = await Updater.UpdateAsync(context).ConfigureAwait(false);
                if (changeDescription is not null)
                {
                    Log.CreatingChangelist(logger, project.Id, project.Name);
                    var changelist = await project.CreateChangelistAsync(new(localRepository, options, changeDescription, cancellationToken)).ConfigureAwait(false);

                    await database.UpdateProject(project, changelist, exception: null).ConfigureAwait(false);
                    Log.ProjectProcessed(logger, project.Id, project.Name, changelist.CommitId, changelist.BranchName, changelist.ReviewUrl);

                    if (OpenReviewUrlInBrowser && changelist.ReviewUrl is not null)
                    {
                        Process.Start(new ProcessStartInfo(changelist.ReviewUrl.ToString()) { UseShellExecute = true });
                    }

                    rateLimiter.UpdateCompleted(changelist);
                }
                else
                {
                    Log.ProjectProcessedWithoutChanges(logger, project.Id, project.Name);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorWhileProcessingProject(logger, ex, project.Id, project.Name, ex.Message);
                await database.UpdateProject(project, pushResult: null, ex.ToString()).ConfigureAwait(false);
            }
            finally
            {
                await temporaryDirectory.DisposeAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }
}
