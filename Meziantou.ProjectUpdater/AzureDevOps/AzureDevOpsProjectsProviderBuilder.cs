using System.Runtime.CompilerServices;
using System.Text;
using CliWrap;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Meziantou.ProjectUpdater.AzureDevOps;

public sealed class AzureDevOpsProjectsProviderBuilder
{
    private string? _personalAccessToken;
    private readonly List<Func<VssConnection, CancellationToken, IAsyncEnumerable<Project>>> _providers = [];
    private readonly List<Func<IAsyncEnumerable<Project>, IAsyncEnumerable<Project>>> _filters = [];
    private bool _includeDisabled = true;
    private bool _includeForks = true;
    private Uri? _collection;

    public AzureDevOpsProjectsProviderBuilder Authenticate(string? personalAccessToken)
    {
        _personalAccessToken = personalAccessToken;
        return this;
    }

    public AzureDevOpsProjectsProviderBuilder SetCollection(Uri uri)
    {
        _collection = uri;
        return this;
    }

    public AzureDevOpsProjectsProviderBuilder AddAccessibleRepositories()
    {
        _providers.Add(Convert(async (connection, cancellationToken) =>
        {
            var client = await connection.GetClientAsync<GitHttpClient>(cancellationToken).ConfigureAwait(false);
            var repositories = await client.GetRepositoriesAsync(includeLinks: null, userState: null, cancellationToken).ConfigureAwait(false);
            return repositories;
        }));
        return this;
    }

    public AzureDevOpsProjectsProviderBuilder ExcludeDisabledRepositories()
    {
        _includeDisabled = false;
        return this;
    }

    public AzureDevOpsProjectsProviderBuilder ExcludeForks()
    {
        _includeForks = false;
        return this;
    }

    internal Func<IAsyncEnumerable<Project>> Build()
    {
        return () =>
        {
            var result = CombineProjects();

            if (!_includeDisabled)
                result = result.Where(p => !((AzureDevOpsGitProject)p).IsDisabled);

            if (!_includeForks)
                result = result.Where(p => !((AzureDevOpsGitProject)p).IsFork);

            foreach (var filter in _filters)
            {
                result = filter(result);
            }

            return result;
        };

        async IAsyncEnumerable<Project> CombineProjects([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var token = await GetToken(cancellationToken).ConfigureAwait(false);
            var creds = new VssBasicCredential(string.Empty, token);
            var connection = new VssConnection(_collection, creds);

            foreach (var provider in _providers)
            {
                await foreach (var project in provider(connection, cancellationToken).ConfigureAwait(false))
                {
                    yield return project;
                }
            }
        }
    }

    private Func<VssConnection, CancellationToken, IAsyncEnumerable<Project>> Convert(Func<VssConnection, CancellationToken, Task<List<GitRepository>>> func)
    {
        return ConvertCore;

        async IAsyncEnumerable<Project> ConvertCore(VssConnection connection, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = await connection.GetClientAsync<GitHttpClient>(cancellationToken).ConfigureAwait(false);
            var response = await func(connection, cancellationToken).ConfigureAwait(false);
            foreach (var item in response)
            {
                yield return new AzureDevOpsGitProject(client, item, _personalAccessToken);
            }
        }
    }

    private async ValueTask<string?> GetToken(CancellationToken cancellationToken)
    {
        var token = _personalAccessToken;
        if (token is null)
        {
            try
            {
                var sb = new StringBuilder();
                await Cli.Wrap("az")
                    .WithArguments(["account", "get-access-token", "--resource", "499b84ac-1321-427f-aa17-267ca6975798", "--query", "accessToken", "--output", "tsv"])
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sb))
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                token = sb.ToString().Trim(' ', '\r', '\n');
                _personalAccessToken = token;
            }
            catch
            {
            }
        }

        return token;
    }
}