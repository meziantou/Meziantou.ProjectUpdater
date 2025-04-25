using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

[SuppressMessage("Design", "CA1812")]
internal sealed class GitHubRepoSimpleUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
