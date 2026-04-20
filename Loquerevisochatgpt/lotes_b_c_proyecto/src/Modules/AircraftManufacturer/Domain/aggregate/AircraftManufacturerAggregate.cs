namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.ValueObject;

/// <summary>
/// Fabricante de aeronaves. Pertenece a un país (country_id).
/// Tabla SQL: aircraft_manufacturer. PK: manufacturer_id.
/// </summary>
public sealed class AircraftManufacturerAggregate
{
    public AircraftManufacturerId Id        { get; private set; }
    public string                 Name      { get; private set; } = string.Empty;
    public int                    CountryId { get; private set; }

    private AircraftManufacturerAggregate() { }

    public static AircraftManufacturerAggregate Create(string name, int countryId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 100)               throw new ArgumentException("Name cannot exceed 100 characters.", nameof(name));
        if (countryId <= 0)                  throw new ArgumentException("CountryId must be positive.", nameof(countryId));
        return new AircraftManufacturerAggregate { Name = name.Trim(), CountryId = countryId };
    }

    public static AircraftManufacturerAggregate Reconstitute(int id, string name, int countryId) =>
        new() { Id = AircraftManufacturerId.New(id), Name = name, CountryId = countryId };

    public void Update(string name, int countryId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 100)               throw new ArgumentException("Name cannot exceed 100 characters.", nameof(name));
        if (countryId <= 0)                  throw new ArgumentException("CountryId must be positive.", nameof(countryId));
        Name = name.Trim(); CountryId = countryId;
    }
}
