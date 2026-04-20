namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public sealed class CancellationReasonId
{
    public int Value { get; }

    public CancellationReasonId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CancellationReasonId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CancellationReasonId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
