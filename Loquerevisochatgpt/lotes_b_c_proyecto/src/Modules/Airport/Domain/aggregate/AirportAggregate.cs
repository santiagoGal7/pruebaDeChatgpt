namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.ValueObject;

/// <summary>
/// Agregado raíz del módulo Airport.
/// Encapsula las reglas de negocio de un aeropuerto.
/// IATA code: exactamente 3 letras mayúsculas.
/// </summary>
public sealed class AirportAggregate
{
    public AirportId Id       { get; private set; }
    public string    IataCode { get; private set; } = string.Empty;
    public string    Name     { get; private set; } = string.Empty;
    public int       CityId   { get; private set; }
    public DateTime  CreatedAt { get; private set; }

    private AirportAggregate() { }

    public static AirportAggregate Create(string iataCode, string name, int cityId)
    {
        ValidateIataCode(iataCode);
        ValidateName(name);
        if (cityId <= 0) throw new ArgumentException("CityId must be positive.", nameof(cityId));

        return new AirportAggregate
        {
            IataCode  = iataCode.ToUpperInvariant(),
            Name      = name.Trim(),
            CityId    = cityId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AirportAggregate Reconstitute(int id, string iataCode, string name, int cityId, DateTime createdAt) =>
        new()
        {
            Id        = AirportId.New(id),
            IataCode  = iataCode,
            Name      = name,
            CityId    = cityId,
            CreatedAt = createdAt
        };

    public void Update(string iataCode, string name, int cityId)
    {
        ValidateIataCode(iataCode);
        ValidateName(name);
        if (cityId <= 0) throw new ArgumentException("CityId must be positive.", nameof(cityId));

        IataCode = iataCode.ToUpperInvariant();
        Name     = name.Trim();
        CityId   = cityId;
    }

    private static void ValidateIataCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3 || !code.All(char.IsLetter))
            throw new ArgumentException("IATA code must be exactly 3 letters.", nameof(code));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Airport name cannot be empty.", nameof(name));
        if (name.Length > 150)
            throw new ArgumentException("Airport name cannot exceed 150 characters.", nameof(name));
    }
}
