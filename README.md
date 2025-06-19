# Meziantou.ProjectUpdater

This console application allows you to automate changes across multiple repositories. It currently supports GitHub and Azure DevOps (git).

- Create an `IProjectUpdater` that applies the change to a repository. You can find examples in the `Meziantou.ProjectUpdater.Console` project.
- Update the `Program.cs` to select the projects to update and the `IProjectUpdater` to apply.
