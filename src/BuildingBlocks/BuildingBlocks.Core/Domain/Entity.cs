using BuildingBlocks.Core.Validations;

namespace BuildingBlocks.Core.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = id;
    }
    
    public abstract ValidationHandler Validate();
    
    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }

}