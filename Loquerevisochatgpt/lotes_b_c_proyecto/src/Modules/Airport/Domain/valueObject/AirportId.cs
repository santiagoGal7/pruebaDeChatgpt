namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.ValueObject;

/// <summary>
/// Value Object que representa el identificador único de un aeropuerto.
/// </summary>
public readonly record struct AirportId(int Value)
{
    public static AirportId New(int value)
    {
        if (value <= 0)
            throw new ArgumentException("AirportId must be a positive integer.", nameof(value));
        return new AirportId(value);
    }

    public override string ToString() => Value.ToString();
}
