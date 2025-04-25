namespace Meziantou.ProjectUpdater.GitHub.Client.Authentication;

internal sealed class BearerTokenAuthenticator(string token) : IAuthenticator
{
    public string Token { get; } = token ?? throw new ArgumentNullException(nameof(token));

    public ValueTask AuthenticateAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        return ValueTask.CompletedTask;
    }
}
