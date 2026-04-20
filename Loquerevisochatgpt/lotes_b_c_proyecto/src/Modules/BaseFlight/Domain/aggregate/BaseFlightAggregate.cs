namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

/// <summary>
/// Vuelo base: código IATA de vuelo + aerolínea + ruta.
/// Representa la plantilla recurrente sin instancia concreta de fecha/hora.
/// Invariante: flight_code debe tener al menos 2 caracteres (CHECK SQL).
/// UNIQUE: (flight_code, airline_id).
/// </summary>
public sealed class BaseFlightAggregate
{
    public BaseFlightId Id         { get; private set; }
    public string       FlightCode { get; private set; }
    public int          AirlineId  { get; private set; }
    public int          RouteId    { get; private set; }
    public DateTime     CreatedAt  { get; private set; }
    public DateTime?    UpdatedAt  { get; private set; }

    private BaseFlightAggregate()
    {
        Id         = null!;
        FlightCode = null!;
    }

    public BaseFlightAggregate(
        BaseFlightId id,
        string       flightCode,
        int          airlineId,
        int          routeId,
        DateTime     createdAt,
        DateTime?    updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(flightCode))
            throw new ArgumentException("FlightCode cannot be empty.", nameof(flightCode));

        if (flightCode.Trim().Length < 2)
            throw new ArgumentException("FlightCode must be at least 2 characters.", nameof(flightCode));

        if (flightCode.Length > 20)
            throw new ArgumentException("FlightCode cannot exceed 20 characters.", nameof(flightCode));

        if (airlineId <= 0)
            throw new ArgumentException("AirlineId must be a positive integer.", nameof(airlineId));

        if (routeId <= 0)
            throw new ArgumentException("RouteId must be a positive integer.", nameof(routeId));

        Id         = id;
        FlightCode = flightCode.Trim().ToUpperInvariant();
        AirlineId  = airlineId;
        RouteId    = routeId;
        CreatedAt  = createdAt;
        UpdatedAt  = updatedAt;
    }

    /// <summary>
    /// Actualiza los datos modificables del vuelo base.
    /// Registra la fecha de modificación.
    /// </summary>
    public void Update(string flightCode, int airlineId, int routeId)
    {
        if (string.IsNullOrWhiteSpace(flightCode))
            throw new ArgumentException("FlightCode cannot be empty.", nameof(flightCode));

        if (flightCode.Trim().Length < 2)
            throw new ArgumentException("FlightCode must be at least 2 characters.", nameof(flightCode));

        if (flightCode.Length > 20)
            throw new ArgumentException("FlightCode cannot exceed 20 characters.", nameof(flightCode));

        if (airlineId <= 0)
            throw new ArgumentException("AirlineId must be a positive integer.", nameof(airlineId));

        if (routeId <= 0)
            throw new ArgumentException("RouteId must be a positive integer.", nameof(routeId));

        FlightCode = flightCode.Trim().ToUpperInvariant();
        AirlineId  = airlineId;
        RouteId    = routeId;
        UpdatedAt  = DateTime.UtcNow;
    }
}
