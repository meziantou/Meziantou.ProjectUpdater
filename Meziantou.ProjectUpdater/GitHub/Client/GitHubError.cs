using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

[SuppressMessage("Design", "CA1812")]
internal sealed class GitHubError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("documentation_url")]
    public string? DocumentationUrl { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
