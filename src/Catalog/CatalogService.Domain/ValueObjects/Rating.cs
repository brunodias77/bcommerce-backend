using BuildingBlocks.Core.Domain;

namespace CatalogService.Domain.ValueObjects;

public class Rating : ValueObject
{
    public int Value { get; private set; }

    public static Rating OneStar => new(1);
    public static Rating TwoStars => new(2);
    public static Rating ThreeStars => new(3);
    public static Rating FourStars => new(4);
    public static Rating FiveStars => new(5);

    private Rating() { }

    public Rating(int value)
    {
        if (value < 1 || value > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(value));

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}