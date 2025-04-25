using Meziantou.Framework;

namespace Meziantou.ProjectUpdater;

public sealed class BranchName : IEquatable<BranchName?>
{
    public BranchName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    public string Name { get; }

    public static BranchName Slugify(string text)
    {
        var options = new SlugOptions()
        {
            MaximumLength = 100,
            Separator = "-",
            CanEndWithSeparator = false,
            CasingTransformation = CasingTransformation.ToLowerCase,
        };
        return new BranchName(Slug.Create(text, options));
    }

    public override bool Equals(object? obj) => Equals(obj as BranchName);
    public bool Equals(BranchName? other) => other is not null && Name == other.Name;
    public override int GetHashCode() => HashCode.Combine(Name);

    public static bool operator ==(BranchName? left, BranchName? right) => EqualityComparer<BranchName>.Default.Equals(left, right);
    public static bool operator !=(BranchName? left, BranchName? right) => !(left == right);

    public override string ToString() => Name;
}