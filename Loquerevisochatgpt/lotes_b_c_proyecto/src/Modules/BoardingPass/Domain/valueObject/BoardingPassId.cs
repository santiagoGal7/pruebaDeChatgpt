namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

public sealed class BoardingPassId
{
    public int Value { get; }

    public BoardingPassId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BoardingPassId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BoardingPassId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
