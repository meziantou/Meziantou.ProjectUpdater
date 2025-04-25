using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

public sealed class GitHubPullRequest
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("html_url")]
    public Uri? HtmlUrl { get; set; }
}
