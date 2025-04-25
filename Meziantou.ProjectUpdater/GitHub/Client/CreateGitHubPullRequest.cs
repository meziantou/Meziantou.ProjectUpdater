using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

internal sealed record CreateGitHubPullRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("head")]
    public string? Head { get; set; }

    [JsonPropertyName("head_repo")]
    public string? HeadRepo { get; set; }

    [JsonPropertyName("base")]
    public string? Base { get; set; }
}
