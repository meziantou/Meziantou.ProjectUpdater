namespace Meziantou.ProjectUpdater.GitHub.Client.Authentication;

internal sealed class AnonymousAuthenticator : IAuthenticator
{
    public static AnonymousAuthenticator Instance { get; } = new();

    private AnonymousAuthenticator() { }

    public ValueTask AuthenticateAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
