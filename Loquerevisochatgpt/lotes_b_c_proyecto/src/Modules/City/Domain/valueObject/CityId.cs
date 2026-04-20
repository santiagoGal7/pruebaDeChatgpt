namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.ValueObject;

/// <summary>
/// Value Object que representa el identificador único de una ciudad.
/// </summary>
public readonly record struct CityId(int Value)
{
    public static CityId New(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CityId must be a positive integer.", nameof(value));
        return new CityId(value);
    }

    public override string ToString() => Value.ToString();
}
