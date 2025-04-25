namespace Meziantou.ProjectUpdater.GitHub;

public sealed class GitException : Exception
{
    public GitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    public GitException()
    {
    }

    public GitException(string message) : base(message)
    {
    }
}
