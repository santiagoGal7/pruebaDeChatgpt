namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Domain.ValueObject;

/// <summary>
/// Configuración estática de un tipo de aeronave.
/// SQL: aircraft_type. Regla de negocio: total_seats > 0, cargo_capacity_kg >= 0.
/// </summary>
public sealed class AircraftTypeAggregate
{
    public AircraftTypeId Id               { get; private set; }
    public int            ManufacturerId   { get; private set; }
    public string         Model            { get; private set; } = string.Empty;
    public int            TotalSeats       { get; private set; }
    public decimal        CargoCapacityKg  { get; private set; }

    private AircraftTypeAggregate() { }

    public static AircraftTypeAggregate Create(int manufacturerId, string model, int totalSeats, decimal cargoCapacityKg)
    {
        Validate(manufacturerId, model, totalSeats, cargoCapacityKg);
        return new AircraftTypeAggregate
        {
            ManufacturerId  = manufacturerId,
            Model           = model.Trim(),
            TotalSeats      = totalSeats,
            CargoCapacityKg = cargoCapacityKg
        };
    }

    public static AircraftTypeAggregate Reconstitute(int id, int manufacturerId, string model, int totalSeats, decimal cargoCapacityKg) =>
        new()
        {
            Id              = AircraftTypeId.New(id),
            ManufacturerId  = manufacturerId,
            Model           = model,
            TotalSeats      = totalSeats,
            CargoCapacityKg = cargoCapacityKg
        };

    public void Update(int manufacturerId, string model, int totalSeats, decimal cargoCapacityKg)
    {
        Validate(manufacturerId, model, totalSeats, cargoCapacityKg);
        ManufacturerId  = manufacturerId;
        Model           = model.Trim();
        TotalSeats      = totalSeats;
        CargoCapacityKg = cargoCapacityKg;
    }

    private static void Validate(int manufacturerId, string model, int totalSeats, decimal cargo)
    {
        if (manufacturerId <= 0)               throw new ArgumentException("ManufacturerId must be positive.", nameof(manufacturerId));
        if (string.IsNullOrWhiteSpace(model))  throw new ArgumentException("Model cannot be empty.", nameof(model));
        if (model.Length > 50)                 throw new ArgumentException("Model cannot exceed 50 characters.", nameof(model));
        if (totalSeats <= 0)                   throw new ArgumentException("TotalSeats must be greater than 0.", nameof(totalSeats));
        if (cargo < 0)                         throw new ArgumentException("CargoCapacityKg cannot be negative.", nameof(cargo));
    }
}
