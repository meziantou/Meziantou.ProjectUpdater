using Microsoft.Extensions.Logging;

namespace Meziantou.ProjectUpdater;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing project '{ProjectId}' ({ProjectName})")]
    public static partial void ProcessingProject(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project '{ProjectId}' ({ProjectName}) is already processed")]
    public static partial void ProjectAlreadyProcessed(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cloning project '{ProjectId}' ({ProjectName})")]
    public static partial void CloningProject(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updating project '{ProjectId}' ({ProjectName})")]
    public static partial void UpdatingProject(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating changelist for project '{ProjectId}' ({ProjectName})")]
    public static partial void CreatingChangelist(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project '{ProjectId}' ({ProjectName}) is completed. Changelist: {CommitId}; BranchName: {BranchName}; URL: {ReviewUrl}")]
    public static partial void ProjectProcessed(ILogger logger, string projectId, string projectName, string commitId, string branchName, Uri? reviewUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project '{ProjectId}' ({ProjectName}) is completed without changelist")]
    public static partial void ProjectProcessedWithoutChanges(ILogger logger, string projectId, string projectName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error while updating project '{ProjectId}' ({ProjectName}): {Message}")]
    public static partial void ErrorWhileProcessingProject(ILogger logger, Exception exception, string projectId, string projectName, string message);
}
