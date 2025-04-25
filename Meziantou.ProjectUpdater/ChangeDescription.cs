namespace Meziantou.ProjectUpdater;

public sealed class ChangeDescription
{
    public ChangeDescription(string title, string? description)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);

        Title = title;
        Description = description;
    }

    public string Title { get; }
    public string? Description { get; }

    public BranchName? BranchName { get; set; }
}