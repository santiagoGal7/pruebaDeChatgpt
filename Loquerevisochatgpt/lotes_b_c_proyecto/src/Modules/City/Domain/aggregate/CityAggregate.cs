namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.ValueObject;

/// <summary>
/// Agregado raíz del módulo City.
/// Encapsula las reglas de negocio de una ciudad.
/// </summary>
public sealed class CityAggregate
{
    public CityId Id { get; private set; }
    public string Name { get; private set; }
    public int CountryId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CityAggregate() { }

    /// <summary>Crea una nueva instancia de ciudad (sin persistir).</summary>
    public static CityAggregate Create(string name, int countryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("City name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("City name cannot exceed 100 characters.", nameof(name));
        if (countryId <= 0)
            throw new ArgumentException("CountryId must be a positive integer.", nameof(countryId));

        return new CityAggregate
        {
            Name      = name.Trim(),
            CountryId = countryId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Reconstituye el agregado desde persistencia.</summary>
    public static CityAggregate Reconstitute(int id, string name, int countryId, DateTime createdAt)
    {
        return new CityAggregate
        {
            Id        = CityId.New(id),
            Name      = name,
            CountryId = countryId,
            CreatedAt = createdAt
        };
    }

    /// <summary>Actualiza los datos modificables de la ciudad.</summary>
    public void Update(string name, int countryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("City name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("City name cannot exceed 100 characters.", nameof(name));
        if (countryId <= 0)
            throw new ArgumentException("CountryId must be a positive integer.", nameof(countryId));

        Name      = name.Trim();
        CountryId = countryId;
    }
}
