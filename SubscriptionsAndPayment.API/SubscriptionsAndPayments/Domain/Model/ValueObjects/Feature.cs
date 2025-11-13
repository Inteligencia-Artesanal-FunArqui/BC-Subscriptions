namespace OsitoPolar.Subscriptions.Service.Domain.Model.ValueObjects;

public class Feature
{
    public string Name { get; }

    public Feature(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public override bool Equals(object? obj) => obj is Feature other && Name == other.Name;
    public override int GetHashCode() => Name.GetHashCode();
}