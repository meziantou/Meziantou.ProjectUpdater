using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

[SuppressMessage("Design", "CA1812")]
internal sealed class GitHubRepository : GitHubMinimalRepository
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
