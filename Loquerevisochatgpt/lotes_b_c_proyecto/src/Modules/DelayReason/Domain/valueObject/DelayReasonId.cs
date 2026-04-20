namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public sealed class DelayReasonId
{
    public int Value { get; }

    public DelayReasonId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("DelayReasonId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is DelayReasonId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
