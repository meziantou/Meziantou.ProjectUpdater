using System.Text.Json;
using System.Text.Json.Serialization;
using Meziantou.Framework;
using Meziantou.ProjectUpdater;

namespace Meziantou.BatchUpdates;

internal sealed partial class Database : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FullPath _path;
    private readonly CancellationToken _cancellationToken;

    [JsonInclude]
    private List<UpdateStatusProject> Projects { get; set; } = [];

    private Database(FullPath path, CancellationToken cancellationToken)
    {
        _path = path;
        _cancellationToken = cancellationToken;
    }

    public void Dispose() => _lock.Dispose();

    public static async Task<Database> Load(FullPath path, CancellationToken cancellationToken)
    {
        if (!path.IsEmpty)
        {
            try
            {
                var stream = File.OpenRead(path);
                await using (stream.ConfigureAwait(false))
                {
                    var result = await JsonSerializer.DeserializeAsync<Database>(stream, SourceGenerationContext.Default.Database, cancellationToken).ConfigureAwait(false);
                    if (result is not null)
                        return result;
                }
            }
            catch
            {
            }
        }

        return new Database(path, cancellationToken);
    }

    private async Task Save()
    {
        if (_path.IsEmpty)
            return;

        var stream = File.Create(_path);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, this, SourceGenerationContext.Default.Database, _cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddProject(Project project)
    {
        var projectId = project.Id;
        await _lock.WaitAsync(_cancellationToken).ConfigureAwait(false);
        try
        {
            var existingProject = Projects.FirstOrDefault(p => p.IsProject(project));
            if (existingProject is null)
            {
                Projects.Add(new UpdateStatusProject
                {
                    ProjectId = projectId,
                    IsProcessed = false,
                });
            }

            await Save().ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateProject(Project project, ChangelistInformation? pushResult, string? exception)
    {
        await _lock.WaitAsync(_cancellationToken).ConfigureAwait(false);
        try
        {
            var existingProject = Projects.FirstOrDefault(p => p.IsProject(project));
            if (existingProject is null)
                throw new InvalidOperationException($"Project {project.Name} not found in the database.");

            existingProject.IsProcessed = true;
            existingProject.ErrorMessage = exception;
            existingProject.CommitId = pushResult?.CommitId;
            existingProject.ReviewUrl = pushResult?.ReviewUrl;
            existingProject.FinishedAt = DateTimeOffset.UtcNow;
            await Save().ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsProcessed(Project project)
    {
        await _lock.WaitAsync(_cancellationToken).ConfigureAwait(false);
        try
        {
            return Projects.Exists(p => p.IsProcessed && p.IsProject(project));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyCollection<string>> GetAllErrors()
    {
        await _lock.WaitAsync(_cancellationToken).ConfigureAwait(false);
        try
        {
            return Projects.Select(p => p.ErrorMessage).Where(p => !string.IsNullOrEmpty(p)).ToArray()!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class UpdateStatusProject
    {
        public bool IsProject(Project project) => ProjectId == project.Id;

        public string? ProjectId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CommitId { get; set; }
        public Uri? ReviewUrl { get; set; }
        public bool IsProcessed { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
    }

    [JsonSourceGenerationOptions]
    [JsonSerializable(typeof(Database))]
    private sealed partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}