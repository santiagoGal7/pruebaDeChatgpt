namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.ValueObject;

public readonly record struct AircraftId(int Value)
{
    public static AircraftId New(int value)
    {
        if (value <= 0) throw new ArgumentException("AircraftId must be positive.", nameof(value));
        return new AircraftId(value);
    }
    public override string ToString() => Value.ToString();
}
