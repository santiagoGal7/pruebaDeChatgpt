namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.ValueObject;

public readonly record struct AirlineId(int Value)
{
    public static AirlineId New(int value)
    {
        if (value <= 0) throw new ArgumentException("AirlineId must be positive.", nameof(value));
        return new AirlineId(value);
    }
    public override string ToString() => Value.ToString();
}
