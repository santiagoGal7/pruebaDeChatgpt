namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public sealed class CheckInStatusId
{
    public int Value { get; }

    public CheckInStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CheckInStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CheckInStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
