namespace Meziantou.ProjectUpdater.GitHub.Client.Authentication;

internal interface IAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage message, CancellationToken cancellationToken);
}
