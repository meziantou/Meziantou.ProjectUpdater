using System.Text.Json.Serialization;

namespace Meziantou.ProjectUpdater.GitHub.Client;

// search "minimal-repository:" (line 69001) in https://raw.githubusercontent.com/github/rest-api-description/695e5f12e7646ea4c5c2dfd771179939eac2e4b5/descriptions/api.github.com/api.github.com.2022-11-28.yaml
internal class GitHubMinimalRepository
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("clone_url")]
    public string? CloneUrl { get; set; }

    [JsonPropertyName("ssh_url")]
    public string? SshUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("fork")]
    public bool Fork { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("has_issues")]
    public bool HasIssues { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    [JsonPropertyName("stargazers_count")]
    public long StargazersCount { get; set; }

    [JsonPropertyName("watchers_count")]
    public long WatchersCount { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("forks")]
    public long Forks { get; set; }

    [JsonPropertyName("default_branch")]
    public string? DefaultBranch { get; set; }

    [JsonPropertyName("owner")]
    public GitHubRepoSimpleUser? Owner { get; set; }
}
