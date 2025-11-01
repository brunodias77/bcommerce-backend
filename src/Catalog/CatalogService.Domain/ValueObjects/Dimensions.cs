using BuildingBlocks.Core.Domain;

namespace CatalogService.Domain.ValueObjects;

public class Dimensions : ValueObject
{
    public decimal Length { get; private set; }
    public decimal Width { get; private set; }
    public decimal Height { get; private set; }
    public string Unit { get; private set; }

    private Dimensions()
    {
        Unit = "cm";
    }

    private Dimensions(decimal length, decimal width, decimal height, string unit = "cm")
    {
        if (length <= 0 || width <= 0 || height <= 0)
            throw new ArgumentException("Dimensions must be positive");

        Length = length;
        Width = width;
        Height = height;
        Unit = unit;
    }

    public static Dimensions Create(decimal length, decimal width, decimal height, string unit = "cm")
    {
        if (length <= 0 || width <= 0 || height <= 0)
            throw new ArgumentException("Dimensions must be positive");

        return new Dimensions(length, width, height, unit);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Length;
        yield return Width;
        yield return Height;
        yield return Unit;
    }

    public decimal Volume() => Length * Width * Height;

    public override string ToString() => $"{Length}x{Width}x{Height} {Unit}";

    public static Dimensions? FromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split('x');
        if (parts.Length != 3)
            return null;

        if (decimal.TryParse(parts[0], out var length) &&
            decimal.TryParse(parts[1], out var width) &&
            decimal.TryParse(parts[2], out var height))
        {
            return Create(length, width, height);
        }

        return null;
    }
}