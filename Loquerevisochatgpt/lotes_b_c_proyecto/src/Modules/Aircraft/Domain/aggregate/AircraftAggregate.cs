namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.ValueObject;

/// <summary>
/// Aeronave física con matrícula única.
/// SQL: aircraft. Regla: manufacture_year plausible.
/// NOTA: manufacture_year en SQL es tipo YEAR (int en C#).
/// </summary>
public sealed class AircraftAggregate
{
    public AircraftId  Id                   { get; private set; }
    public int         AirlineId            { get; private set; }
    public int         AircraftTypeId       { get; private set; }
    public string      RegistrationNumber   { get; private set; } = string.Empty;
    public int         ManufactureYear      { get; private set; }
    public bool        IsActive             { get; private set; }
    public DateTime    CreatedAt            { get; private set; }
    public DateTime?   UpdatedAt            { get; private set; }

    private AircraftAggregate() { }

    public static AircraftAggregate Create(int airlineId, int aircraftTypeId, string registrationNumber, int manufactureYear, bool isActive = true)
    {
        Validate(airlineId, aircraftTypeId, registrationNumber, manufactureYear);
        return new AircraftAggregate
        {
            AirlineId          = airlineId,
            AircraftTypeId     = aircraftTypeId,
            RegistrationNumber = registrationNumber.Trim().ToUpperInvariant(),
            ManufactureYear    = manufactureYear,
            IsActive           = isActive,
            CreatedAt          = DateTime.UtcNow
        };
    }

    public static AircraftAggregate Reconstitute(int id, int airlineId, int aircraftTypeId, string registrationNumber, int manufactureYear, bool isActive, DateTime createdAt, DateTime? updatedAt) =>
        new()
        {
            Id                 = AircraftId.New(id),
            AirlineId          = airlineId,
            AircraftTypeId     = aircraftTypeId,
            RegistrationNumber = registrationNumber,
            ManufactureYear    = manufactureYear,
            IsActive           = isActive,
            CreatedAt          = createdAt,
            UpdatedAt          = updatedAt
        };

    public void Update(int airlineId, int aircraftTypeId, string registrationNumber, int manufactureYear, bool isActive)
    {
        Validate(airlineId, aircraftTypeId, registrationNumber, manufactureYear);
        AirlineId          = airlineId;
        AircraftTypeId     = aircraftTypeId;
        RegistrationNumber = registrationNumber.Trim().ToUpperInvariant();
        ManufactureYear    = manufactureYear;
        IsActive           = isActive;
        UpdatedAt          = DateTime.UtcNow;
    }

    private static void Validate(int airlineId, int aircraftTypeId, string regNum, int year)
    {
        if (airlineId <= 0)                   throw new ArgumentException("AirlineId must be positive.", nameof(airlineId));
        if (aircraftTypeId <= 0)              throw new ArgumentException("AircraftTypeId must be positive.", nameof(aircraftTypeId));
        if (string.IsNullOrWhiteSpace(regNum)) throw new ArgumentException("RegistrationNumber cannot be empty.", nameof(regNum));
        if (regNum.Length > 20)               throw new ArgumentException("RegistrationNumber cannot exceed 20 characters.", nameof(regNum));
        if (year < 1900 || year > DateTime.UtcNow.Year + 2)
            throw new ArgumentException($"ManufactureYear must be between 1900 and {DateTime.UtcNow.Year + 2}.", nameof(year));
    }
}
