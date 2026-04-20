namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.ValueObject;

public readonly record struct AircraftManufacturerId(int Value)
{
    public static AircraftManufacturerId New(int value)
    {
        if (value <= 0) throw new ArgumentException("AircraftManufacturerId must be positive.", nameof(value));
        return new AircraftManufacturerId(value);
    }
    public override string ToString() => Value.ToString();
}
