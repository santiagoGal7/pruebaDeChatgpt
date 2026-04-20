namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public sealed class CheckInId
{
    public int Value { get; }

    public CheckInId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CheckInId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CheckInId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
