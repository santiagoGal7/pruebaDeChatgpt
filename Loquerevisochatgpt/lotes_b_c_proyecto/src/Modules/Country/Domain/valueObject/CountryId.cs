namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;

public sealed class CountryId
{
    public int Value { get; }

    public CountryId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CountryId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CountryId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
