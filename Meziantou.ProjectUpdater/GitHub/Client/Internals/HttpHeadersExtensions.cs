using System.Net.Http.Headers;
using System.Globalization;

namespace Meziantou.ProjectUpdater.GitHub.Client.Internals;

internal static class HttpHeadersExtensions
{
    public static int GetHeaderValue(this HttpHeaders headers, string name, int defaultValue)
    {
        if (headers.TryGetValues(name, out var enumerable))
        {
            var value = enumerable.First();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
        }

        return defaultValue;
    }
}
