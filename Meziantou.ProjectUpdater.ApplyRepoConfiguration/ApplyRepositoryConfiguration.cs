using System.Diagnostics;
using CliWrap;
using Meziantou.Framework;
using Microsoft.Extensions.Logging;

namespace Meziantou.ProjectUpdater.Console.Updaters;
internal sealed class ApplyRepositoryConfiguration : IProjectUpdater
{
    private static readonly Lazy<Task> InstallTools = new(() => Cli.Wrap("dotnet").WithArguments(["tool", "update", "Meziantou.FileReferencer", "--global"]).ExecuteAsync());

    public async ValueTask<ChangeDescription?> UpdateAsync(ProjectUpdaterContext context)
    {
        var repo = context.LocalRepository;
        await AddOrUpdateLicense(repo);
        await AddOrUpdateContributingGuide(context, repo);
        await AddOrUpdateFundingInfo(repo);
        await AddOrUpdateCodeOwners(repo);
        await AddOrUpdateWorkflows(repo);
        await AddOrUpdateEditorConfig(repo);

        await InstallTools.Value;
        var path = ExecutableFinder.GetFullExecutablePath("Meziantou.FileReferencer");
        Debug.Assert(path is not null);

        await Cli.Wrap(path).WithArguments(["--recurse", repo.RootPath])
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => context.Logger.LogInformation("{StdOut}", line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => context.Logger.LogInformation("{StdErr}", line)))
            .ExecuteAsync();

        return new ChangeDescription("Apply repository configuration")
        {
            BranchName = new BranchName("apply-repository-configuration"),
        };
    }

    private static async Task AddOrUpdateContributingGuide(ProjectUpdaterContext context, LocalRepository repo)
    {
        await repo.AddFileAsync("CONTRIBUTING.md", $$"""
            # How to contribute

            Please read these guidelines before contributing to {{context.Project.Name}}:

             - [Question or Problem?](#got-a-question-or-problem)
             - [Issues and Bugs](#found-an-issue)
             - [Feature Requests](#want-a-feature)
             - [Submitting a Pull Request](#submitting-a-pull-request)
             - [Contributor License Agreement](#contributor-license-agreement)


            ## Got a Question or Problem?

            If you have questions about how to use {{context.Project.Name}}, please open a GitHub issue.

            ## Found an Issue?

            If you find a bug in the source code, you can help by submitting an issue to the
            GitHub Repository. Even better you can submit a Pull Request with a fix.

            When submitting an issue please include the following information:

            - A description of the issue
            - The exception message and stacktrace if an error was thrown
            - If possible, please include code that reproduces the issue. DropBox or GitHub's
            Gist can be used to share large code samples, or you could submit a pull request
            with the issue reproduced in a new test.

            The more information you include about the issue, the more likely it is to be fixed!

            ## Want a Feature?

            You can request a new feature by submitting an issue to the GitHub Repository.

            ## Submitting a Pull Request

            When submitting a pull request to the GitHub Repository make sure to do the following:

            - Check that new and updated code follows {{context.Project.Name}}'s existing code formatting and naming standard
            - Run {{context.Project.Name}}'s unit tests to ensure no existing functionality has been affected
            - Write new unit tests to test your changes. All features and fixed bugs must have tests to verify they work

            Read GitHub Help for more details about creating pull requests.

            ## Contributor License Agreement

            By contributing your code to {{context.Project.Name}} you grant Gérald Barré a non-exclusive, irrevocable, worldwide,
            royalty-free, sublicenseable, transferable license under all of Your relevant intellectual property rights
            (including copyright, patent, and any other rights), to use, copy, prepare derivative works of, distribute and
            publicly perform and display the Contributions on any licensing terms, including without limitation:
            (a) open source licenses like the MIT license; and (b) binary, proprietary, or commercial licenses. Except for the
            licenses granted herein, You reserve all right, title, and interest in and to the Contribution.

            You confirm that you are able to grant us these rights. You represent that You are legally entitled to grant the
            above license. If Your employer has rights to intellectual property that You create, You represent that You have
            received permission to make the Contributions on behalf of that employer, or that Your employer has waived such
            rights for the Contributions.

            You represent that the Contributions are Your original works of authorship, and to Your knowledge, no other person
            claims, or has the right to claim, any right in any invention or patent related to the Contributions. You also
            represent that You are not legally obligated, whether by entering into an agreement or otherwise, in any way that
            conflicts with the terms of this license.

            Gérald Barré acknowledges that, except as explicitly described in this Agreement, any Contribution which
            you provide is on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
            INCLUDING, WITHOUT LIMITATION, ANY WARRANTIES OR CONDITIONS OF TITLE, NON-INFRINGEMENT, MERCHANTABILITY, OR FITNESS
            FOR A PARTICULAR PURPOSE.
            """);
    }

    private static async Task AddOrUpdateFundingInfo(LocalRepository repo)
    {
        await repo.AddFileAsync(".github/FUNDING.yml", """
            # https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/displaying-a-sponsor-button-in-your-repository
            github: [meziantou]
            buy_me_a_coffee: meziantou
            """);

        DeleteFile(repo, ".github/FUNDING.yaml");
    }

    private static async Task AddOrUpdateCodeOwners(LocalRepository repo)
    {
        await repo.AddFileAsync("CODEOWNERS", """
            * @meziantou
            """);
    }

    private static async Task AddOrUpdateLicense(LocalRepository repo)
    {
        await repo.AddFileAsync("LICENSE.txt", """
            MIT License

            Copyright (c) Gérald Barré

            Permission is hereby granted, free of charge, to any person obtaining a copy
            of this software and associated documentation files (the "Software"), to deal
            in the Software without restriction, including without limitation the rights
            to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
            copies of the Software, and to permit persons to whom the Software is
            furnished to do so, subject to the following conditions:

            The above copyright notice and this permission notice shall be included in all
            copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
            IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
            FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
            AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
            OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
            SOFTWARE.
            """);

        DeleteFile(repo, "LICENSE.md");
    }

    private static async Task AddOrUpdateWorkflows(LocalRepository repo)
    {
        await repo.AddFileAsync(".github/workflows/close-issues.yml", """
            name: Close inactive issues
            on:
              workflow_dispatch:
              schedule:
                - cron: "30 1 * * *"

            jobs:
              close-issues:
                runs-on: ubuntu-latest
                permissions:
                  issues: write
                  pull-requests: write
                steps:
                  - uses: actions/stale@v9
                    with:
                      days-before-issue-stale: 60
                      days-before-issue-close: 14
                      stale-issue-label: "stale"
                      stale-issue-message: "This issue is stale because it has been open for 60 days with no activity."
                      close-issue-message: "This issue was closed because it has been inactive for 14 days since being marked as stale."
                      days-before-pr-stale: 60
                      days-before-pr-close: 60
                      stale-pr-message: "This pull request is stale because it has been open for 60 days with no activity."
                      close-pr-message: "This pull request was closed because it has been inactive for 14 days since being marked as stale."
                      repo-token: ${{ secrets.GITHUB_TOKEN }}
            """);
    }

    private static async Task AddOrUpdateEditorConfig(LocalRepository repo)
    {
        await repo.AddOrUpdateFileAsync(".editorconfig", content =>
        {
            var suffix = "";
            if (content != null)
            {
                var index = content.IndexOf("[*.cs]");
                if (index >= 0)
                {
                    suffix = content.Substring(index);
                }
            }

            return $$"""
                # reference:https://raw.githubusercontent.com/meziantou/Meziantou.DotNet.CodingStandard/refs/heads/main/.editorconfig
                # endreference

                {{suffix}}
                """;
        });
    }

    private static void DeleteFile(LocalRepository repo, string path)
    {
        try
        {
            File.Delete(repo.RootPath / path);
        }
        catch (FileNotFoundException)
        {
        }

    }
}
