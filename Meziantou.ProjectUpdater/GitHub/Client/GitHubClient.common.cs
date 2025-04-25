using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;
using Meziantou.Framework.Http;
using Meziantou.ProjectUpdater.GitHub.Client.Authentication;
using Meziantou.ProjectUpdater.GitHub.Client.Internals;

namespace Meziantou.ProjectUpdater.GitHub.Client;

internal partial class GitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _httpClientOwned;

    private static readonly ProductInfoHeaderValue UserAgentHeader = new("Meziantou.GitHub", "1.0.0");

    public static readonly Uri GitHubPublicUri = new("https://api.github.com/");

    private Uri ServerUri { get; }
    private IAuthenticator? Authenticator { get; set; }

    private GitHubClient(HttpClient httpClient, bool httpClientOwned, Uri? serverUri, IAuthenticator? authenticator)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _httpClientOwned = httpClientOwned;
        ServerUri = serverUri ?? GitHubPublicUri;
        Authenticator = authenticator;
    }

    public static GitHubClient Create(Uri serverUri)
    {
        return new GitHubClient(new HttpClient(), httpClientOwned: true, serverUri, authenticator: null);
    }

    public static GitHubClient Create(Uri serverUri, string? personalAccessToken)
    {
        return new GitHubClient(new HttpClient(), httpClientOwned: true, serverUri, personalAccessToken is null ? AnonymousAuthenticator.Instance : new BearerTokenAuthenticator(personalAccessToken));
    }

    public static GitHubClient Create(HttpClient httpClient)
    {
        return new GitHubClient(httpClient, httpClientOwned: false, serverUri: null, authenticator: null);
    }

    public static GitHubClient Create(HttpClient httpClient, Uri serverUri, string? personalAccessToken)
    {
        return new GitHubClient(httpClient, httpClientOwned: true, serverUri, personalAccessToken is null ? AnonymousAuthenticator.Instance : new BearerTokenAuthenticator(personalAccessToken));
    }

    private async Task<HttpResponse> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        if (Authenticator is { } authenticator)
        {
            await authenticator.AuthenticateAsync(message, cancellationToken).ConfigureAwait(false);
        }

        if (ServerUri != null && message.RequestUri != null && !message.RequestUri.IsAbsoluteUri)
        {
            message.RequestUri = new Uri(ServerUri, message.RequestUri);
        }

        if (message.Headers.UserAgent.Count is 0 && _httpClient.DefaultRequestHeaders.UserAgent.Count is 0)
        {
            message.Headers.UserAgent.Add(UserAgentHeader);
        }

        var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        return new HttpResponse(this, response);
    }

    internal async Task<GitHubPageResponse<T>> GetPagedCollectionAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.RelativeOrAbsolute));
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        await response.EnsureStatusCodeAsync(cancellationToken).ConfigureAwait(false);

        return await response.ToGitHubPageAsync<T>(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.RelativeOrAbsolute));
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound)
            return default;

        await response.EnsureStatusCodeAsync(cancellationToken).ConfigureAwait(false);
        return await response.ToObjectAsync<T>(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.RelativeOrAbsolute));
        request.Content = JsonContent.Create(data);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);

        await response.EnsureStatusCodeAsync(cancellationToken).ConfigureAwait(false);
        var responseData = await response.ToObjectAsync<TResponse>(cancellationToken).ConfigureAwait(false);
        return responseData ?? throw new GitHubException("Response is empty");
    }

    public void Dispose()
    {
        if (_httpClientOwned)
        {
            _httpClient.Dispose();
        }
    }

    private sealed class HttpResponse(GitHubClient client, HttpResponseMessage message) : IDisposable
    {
        public HttpResponseMessage ResponseMessage { get; } = message;

        public HttpStatusCode StatusCode => ResponseMessage.StatusCode;
        public HttpResponseHeaders ResponseHeaders => ResponseMessage.Headers;
        public HttpMethod? RequestMethod => ResponseMessage.RequestMessage?.Method;
        public Uri? RequestUri => ResponseMessage.RequestMessage?.RequestUri;

        public async Task EnsureStatusCodeAsync(CancellationToken cancellationToken)
        {
            if (ResponseMessage.IsSuccessStatusCode)
                return;

            if (IsJsonResponse(ResponseMessage))
            {
                var error = await DeserializeAsync<GitHubError>(cancellationToken).ConfigureAwait(false);
                throw new GitHubException(RequestMethod, RequestUri, StatusCode, error);
            }
            else
            {
                var error = await ResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new GitHubException(RequestMethod, RequestUri, StatusCode, error);
            }
        }

        public Task<T?> ToObjectAsync<T>(CancellationToken cancellationToken)
        {
            return DeserializeAsync<T>(cancellationToken);
        }

        public Task<IReadOnlyList<T>?> ToCollectionAsync<T>(CancellationToken cancellationToken)
        {
            return DeserializeAsync<IReadOnlyList<T>>(cancellationToken);
        }

        public async Task<HttpResponseStream> ToStreamAsync(CancellationToken cancellationToken)
        {
            var stream = await ResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseStream(stream, ResponseMessage);
        }

        public async Task<GitHubPageResponse<T>> ToGitHubPageAsync<T>(CancellationToken cancellationToken)
        {
            string? firstLink = null;
            string? lastLink = null;
            string? prevLink = null;
            string? nextLink = null;

            var headers = ResponseHeaders;
            foreach (var headerValue in LinkHeaderValue.Parse(headers))
            {
                if (string.Equals(headerValue.Rel, "first", StringComparison.OrdinalIgnoreCase))
                {
                    firstLink = headerValue.Url;
                }
                else if (string.Equals(headerValue.Rel, "last", StringComparison.OrdinalIgnoreCase))
                {
                    lastLink = headerValue.Url;
                }
                else if (string.Equals(headerValue.Rel, "next", StringComparison.OrdinalIgnoreCase))
                {
                    nextLink = headerValue.Url;
                }
                else if (string.Equals(headerValue.Rel, "prev", StringComparison.OrdinalIgnoreCase))
                {
                    prevLink = headerValue.Url;
                }
            }

            var pageIndex = headers.GetHeaderValue("X-Page", -1);
            var pageSize = headers.GetHeaderValue("X-Per-Page", -1);
            var total = headers.GetHeaderValue("X-Total", -1);
            var totalPages = headers.GetHeaderValue("X-Total-Pages", -1);

            var data = await ToCollectionAsync<T>(cancellationToken).ConfigureAwait(false) ?? throw new GitHubException(RequestMethod, RequestUri, StatusCode, $"The response cannot be converted to '{typeof(T)}' collection because the body is null or empty");

            return new GitHubPageResponse<T>(
                client: client,
                data: data,
                pageIndex: pageIndex,
                pageSize: pageSize,
                totalItems: total,
                totalPages: totalPages,
                firstUrl: firstLink,
                previousUrl: prevLink,
                nextUrl: nextLink,
                lastUrl: lastLink);
        }

        public void Dispose()
        {
            ResponseMessage.Dispose();
        }

        private async Task<T?> DeserializeAsync<T>(CancellationToken cancellationToken)
        {
            if (!IsJsonResponse(ResponseMessage))
                throw new InvalidOperationException($"Content type must be application/json but is {ResponseMessage.Content.Headers.ContentType?.MediaType}");

            var s = await ResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await JsonSerialization.DeserializeAsync<T>(s, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await s.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static bool IsJsonResponse(HttpResponseMessage message)
        {
            if (message.Content is null)
                return false;

            return string.Equals(message.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
