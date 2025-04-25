using Meziantou.Framework;

namespace Meziantou.ProjectUpdater;

public abstract class Project : IEquatable<Project?>
{
    protected Project(string id, string name)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }

    public abstract Task CloneAsync(FullPath clonePath, ProjectUpdaterOptions options, CancellationToken cancellationToken = default);
    public abstract Task<ChangelistInformation> CreateChangelistAsync(CreateChangelistRequest request);

    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;

    public override bool Equals(object? obj) => Equals(obj as Project);
    public bool Equals(Project? other) => other is not null && Id == other.Id;
    public override int GetHashCode() => HashCode.Combine(Id);

    public static bool operator ==(Project? left, Project? right) => EqualityComparer<Project>.Default.Equals(left, right);
    public static bool operator !=(Project? left, Project? right) => !(left == right);
}
