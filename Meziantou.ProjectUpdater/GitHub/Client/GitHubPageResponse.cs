namespace Meziantou.ProjectUpdater.GitHub.Client;

internal sealed class GitHubPageResponse<T> : IAsyncEnumerable<T>
{
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }
    private string? PreviousPageUrl { get; }
    private string? NextPageUrl { get; }
    private string? FirstPageUrl { get; }
    private string? LastPageUrl { get; }

    public bool IsLastPage => NextPageUrl is null;
    public bool IsFirstPage => PreviousPageUrl is null;

    internal GitHubClient GitHubClient { get; }
    public IReadOnlyList<T> Data { get; }

    internal GitHubPageResponse(GitHubClient client, IReadOnlyList<T> data, int pageIndex, int pageSize, int totalItems, int totalPages, string? firstUrl, string? lastUrl, string? previousUrl, string? nextUrl)
    {
        GitHubClient = client ?? throw new ArgumentNullException(nameof(client));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = totalPages;
        FirstPageUrl = firstUrl;
        LastPageUrl = lastUrl;
        PreviousPageUrl = previousUrl;
        NextPageUrl = nextUrl;
    }

    public async ValueTask<GitHubPageResponse<T>?> GetFirstPageAsync(CancellationToken cancellationToken = default)
    {
        if (FirstPageUrl is null)
            return null;

        return await GitHubClient.GetPagedCollectionAsync<T>(FirstPageUrl, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<GitHubPageResponse<T>?> GetLastPageAsync(CancellationToken cancellationToken = default)
    {
        if (LastPageUrl is null)
            return null;

        return await GitHubClient.GetPagedCollectionAsync<T>(LastPageUrl, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<GitHubPageResponse<T>?> GetPreviousPageAsync(CancellationToken cancellationToken = default)
    {
        if (PreviousPageUrl is null)
            return null;

        return await GitHubClient.GetPagedCollectionAsync<T>(PreviousPageUrl, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<GitHubPageResponse<T>?> GetNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (NextPageUrl is null)
            return null;

        return await GitHubClient.GetPagedCollectionAsync<T>(NextPageUrl, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var page = this;
        do
        {
            foreach (var item in page.Data)
                yield return item;

            page = await page.GetNextPageAsync(cancellationToken).ConfigureAwait(false);
        } while (page is not null);
    }
}
