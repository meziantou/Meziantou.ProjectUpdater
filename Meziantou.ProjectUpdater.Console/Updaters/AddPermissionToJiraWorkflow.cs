using System.Text;
using Meziantou.Framework;
using YamlDotNet.RepresentationModel;
using static Meziantou.ProjectUpdater.Console.Internals.YamlParserUtilities;

namespace Meziantou.ProjectUpdater.Console.Updaters;

internal class AddPermissionToJiraWorkflow : IProjectUpdater
{
    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        foreach (var file in context.LocalRepository.FindFile(".github/workflows/*.{yml,yaml}"))
        {
            await ProcessFile(context.LocalRepository, file);
        }

        return new ChangeDescription("Add `pull-requests: write` permissions to the jira job", description: "We prepare a change to the shared Jira pipeline. This will require write access to the pull request to edit its description.");
    }

    private static async Task ProcessFile(LocalRepository repository, FullPath path)
    {
        var yaml = LoadYamlDocument(File.ReadAllText(path));
        if (yaml is null)
            return;

        foreach (var document in yaml.Documents)
        {
            if (document.RootNode is not YamlMappingNode rootNode)
                continue;

            var jobsNode = GetPropertyValue(rootNode, "jobs", StringComparison.OrdinalIgnoreCase);
            if (jobsNode is YamlMappingNode jobs)
            {
                foreach (var job in jobs.Children)
                {
                    var uses = GetPropertyValue(job.Value, "uses", StringComparison.Ordinal);
                    var permissions = GetPropertyValue(job.Value, "permissions", StringComparison.Ordinal);
                    var with = GetProperty(job.Value, "with", StringComparison.Ordinal);
                    if (permissions is not null && GetScalarValue(uses)?.Contains("workleap/wl-reusable-workflows/.github/workflows/reusable-jira-workflow.yml", StringComparison.Ordinal) is true)
                    {
                        var child = GetProperty(permissions, "contents", StringComparison.Ordinal);
                        var pullRequest = GetPropertyValue(permissions, "pull-requests", StringComparison.Ordinal);
                        if (pullRequest is null)
                        {
                            // Add pull-requests: write
                            var line = permissions.End.Line;
                            await repository.UpdateFileAsync(path, text =>
                            {
                                var sb = new StringBuilder();
                                var index = 0;
                                foreach (var (line, eol) in text.SplitLines())
                                {
                                    index++;

                                    // Remove "with"
                                    if (with is not null && index >= with.Value.Key.Start.Line && index <= with.Value.Value.End.Line)
                                    {
                                        continue;
                                    }

                                    sb.Append(line).Append(eol);
                                    if (index == child?.Key.End.Line)
                                    {
                                        sb.Append(' ', (int)child?.Key.Start.Column! - 1);
                                        sb.Append($"pull-requests: write").Append(eol);
                                    }
                                }

                                return sb.ToString();
                            });

                            return;
                        }
                        else
                        {
                            throw new NotSupportedException($"Unexpected permissions for pull-requests: {GetScalarValue(pullRequest)}");
                        }
                    }
                }
            }
        }
    }
}
