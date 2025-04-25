using System.Text.Json;

namespace Meziantou.ProjectUpdater.GitHub.Client.Internals;

internal static class JsonSerialization
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = false,
#if DEBUG
        WriteIndented = true,
#endif
    };

    public static ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        return JsonSerializer.DeserializeAsync<T?>(stream, Options, cancellationToken);
    }
}
