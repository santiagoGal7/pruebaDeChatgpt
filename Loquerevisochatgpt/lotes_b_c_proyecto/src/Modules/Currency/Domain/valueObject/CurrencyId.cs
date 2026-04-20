namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public sealed class CurrencyId
{
    public int Value { get; }

    public CurrencyId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CurrencyId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CurrencyId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
