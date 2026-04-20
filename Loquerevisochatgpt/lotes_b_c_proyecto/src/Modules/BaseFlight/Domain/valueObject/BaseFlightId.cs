namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public sealed class BaseFlightId
{
    public int Value { get; }

    public BaseFlightId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BaseFlightId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BaseFlightId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
