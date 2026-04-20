namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.ValueObject;

public readonly record struct AircraftTypeId(int Value)
{
    public static AircraftTypeId New(int value)
    {
        if (value <= 0) throw new ArgumentException("AircraftTypeId must be positive.", nameof(value));
        return new AircraftTypeId(value);
    }
    public override string ToString() => Value.ToString();
}
