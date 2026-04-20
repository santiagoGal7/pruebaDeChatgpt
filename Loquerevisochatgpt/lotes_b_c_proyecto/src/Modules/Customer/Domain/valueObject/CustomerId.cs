namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public sealed class CustomerId
{
    public int Value { get; }

    public CustomerId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CustomerId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CustomerId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
