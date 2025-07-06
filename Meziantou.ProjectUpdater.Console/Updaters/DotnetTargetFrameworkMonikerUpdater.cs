using System.Collections.Concurrent;
using Meziantou.Framework.DependencyScanning;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Meziantou.ProjectUpdater.Console.Updaters;

/// <param name="channel">e.g. 9.0, 10.0</param>
internal sealed class DotnetTargetFrameworkMonikerUpdater(string channel) : IProjectUpdater
{
    private readonly Lazy<Task<string?>> latestSdkVersionCache = new(() => GetLatestSdkVersion(channel));
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> packageVersionCache = new(StringComparer.OrdinalIgnoreCase); // NuGet package name are case insensitive
    private readonly NuGetFramework framework = NuGetFramework.Parse("net" + channel);

    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var updatedCount = 0;
        var dependencies = await DependencyScanner.ScanDirectoryAsync(context.LocalRepository.RootPath, options: null, context.CancellationToken);
        foreach (var dependency in dependencies.OrderByVersionLocationDescending())
        {
            if (dependency.Type is DependencyType.DotNetSdk)
            {
                var latestVersion = await latestSdkVersionCache.Value;
                if (latestVersion is null)
                    continue;

                await UpdateVersion(latestVersion, context.CancellationToken);
            }
            else if (dependency.Type is DependencyType.DotNetTargetFramework)
            {
                // net6.0 => net8.0
                // net6.0-windows => net8.0-windows
                string newValue;
                var index = dependency.Version?.IndexOf('-', StringComparison.Ordinal);
                if (index >= 0)
                {
                    newValue = "net" + channel + dependency.Version![index.Value..];
                }
                else
                {
                    newValue = "net" + channel;
                }

                await UpdateVersion(newValue, context.CancellationToken);
            }
            else if (dependency.Type is DependencyType.NuGet && dependency.Name is not null)
            {
                if (dependency.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                    dependency.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase) ||
                    dependency.Name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase))
                {
                    var latestVersion = await packageVersionCache.GetOrAdd(dependency.Name, _ => new Lazy<Task<string?>>(async () =>
                    {
                        var cache = new SourceCacheContext();
                        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
                        var resource = await repository.GetResourceAsync<PackageMetadataResource>();
                        var metadata = await resource.GetMetadataAsync(dependency.Name, includePrerelease: true, includeUnlisted: false, cache, NuGet.Common.NullLogger.Instance, CancellationToken.None);

                        foreach (var version in metadata.OrderByDescending(m => m.Identity.Version))
                        {
                            if (version.DependencySets.Any(set => DefaultCompatibilityProvider.Instance.IsCompatible(framework, set.TargetFramework)))
                                return version.Identity.Version.ToString();
                        }

                        return null;
                    })).Value;

                    if (latestVersion is not null)
                    {
                        await UpdateVersion(latestVersion, context.CancellationToken).ConfigureAwait(false);
                        break;
                    }
                }
            }
            else if (dependency.Type is DependencyType.DockerImage && dependency.Name is not null)
            {
                if (dependency.Name is "mcr.microsoft.com/dotnet/sdk" or "mcr.microsoft.com/dotnet/aspnet" or "mcr.microsoft.com/dotnet/runtime" or "mcr.microsoft.com/dotnet/runtime-deps")
                {
                    // TODO find latest version of the image + update the suffix if possible
                    // Preserve suffix (e.g. -alpine)
                    var index = dependency.Version?.IndexOf('-', StringComparison.Ordinal);
                    if (index >= 0)
                    {
                        await UpdateVersion(channel + dependency.Version![index.Value..], context.CancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await UpdateVersion(channel, context.CancellationToken).ConfigureAwait(false);
                    }
                }
            }

            async Task UpdateVersion(string newValue, CancellationToken cancellationToken)
            {
                if (dependency.Version == newValue)
                    return;

                await dependency.UpdateVersionAsync(newValue, cancellationToken);
                updatedCount++;
            }
        }

        if (updatedCount == 0)
            return null;

        return new ChangeDescription("Update to .NET " + channel);
    }

    private static async Task<string?> GetLatestSdkVersion(string channel)
    {
        var products = await Microsoft.Deployment.DotNet.Releases.ProductCollection.GetAsync();
        var product = products.FirstOrDefault(p => p.ProductVersion == channel);
        if (product is null)
            return null;

        var releases = await product.GetReleasesAsync();
        var latestRelease = releases.MaxBy(r => r.Version);
        if (latestRelease is null)
            return null;

        var latestVersion = latestRelease.Sdks.MaxBy(sdk => sdk.Version);
        return latestVersion?.Version.ToString();
    }
}
