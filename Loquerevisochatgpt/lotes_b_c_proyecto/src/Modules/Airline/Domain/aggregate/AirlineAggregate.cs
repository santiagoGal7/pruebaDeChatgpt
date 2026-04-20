namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Domain.ValueObject;

/// <summary>
/// Agregado raíz del módulo Airline.
/// IATA code: exactamente 2 letras mayúsculas (IATA airline designator).
/// </summary>
public sealed class AirlineAggregate
{
    public AirlineId Id        { get; private set; }
    public string    IataCode  { get; private set; } = string.Empty;
    public string    Name      { get; private set; } = string.Empty;
    public bool      IsActive  { get; private set; }
    public DateTime  CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private AirlineAggregate() { }

    public static AirlineAggregate Create(string iataCode, string name, bool isActive = true)
    {
        ValidateIataCode(iataCode);
        ValidateName(name);
        return new AirlineAggregate
        {
            IataCode  = iataCode.ToUpperInvariant(),
            Name      = name.Trim(),
            IsActive  = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AirlineAggregate Reconstitute(int id, string iataCode, string name, bool isActive, DateTime createdAt, DateTime? updatedAt) =>
        new()
        {
            Id        = AirlineId.New(id),
            IataCode  = iataCode,
            Name      = name,
            IsActive  = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

    public void Update(string iataCode, string name, bool isActive)
    {
        ValidateIataCode(iataCode);
        ValidateName(name);
        IataCode  = iataCode.ToUpperInvariant();
        Name      = name.Trim();
        IsActive  = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateIataCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2 || !code.All(char.IsLetter))
            throw new ArgumentException("Airline IATA code must be exactly 2 letters.", nameof(code));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Airline name cannot be empty.", nameof(name));
        if (name.Length > 120)               throw new ArgumentException("Airline name cannot exceed 120 characters.", nameof(name));
    }
}
