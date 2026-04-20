namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public sealed class BaggageAllowanceId
{
    public int Value { get; }

    public BaggageAllowanceId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BaggageAllowanceId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BaggageAllowanceId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
