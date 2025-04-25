using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Meziantou.ProjectUpdater.GitHub.Client;

[SuppressMessage("Design", "CA1064:Exceptions should be public")]
internal sealed class GitHubException : Exception
{
    public GitHubException()
    {
    }

    public GitHubException(string message) : base(message)
    {
    }

    public GitHubException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public GitHubException(HttpMethod? httpMethod, Uri? requestUri, HttpStatusCode httpStatusCode, GitHubError? error)
        : base(error?.Message)
    {
        HttpMethod = httpMethod;
        RequestUri = requestUri;
        HttpStatusCode = httpStatusCode;
        GitHubError = error;
    }

    public GitHubException(HttpMethod? httpMethod, Uri? requestUri, HttpStatusCode httpStatusCode, string message)
        : base(message)
    {
        HttpMethod = httpMethod;
        RequestUri = requestUri;
        HttpStatusCode = httpStatusCode;
    }

    public HttpMethod? HttpMethod { get; }
    public Uri? RequestUri { get; }
    public HttpStatusCode HttpStatusCode { get; }
    public GitHubError? GitHubError { get; }
}
