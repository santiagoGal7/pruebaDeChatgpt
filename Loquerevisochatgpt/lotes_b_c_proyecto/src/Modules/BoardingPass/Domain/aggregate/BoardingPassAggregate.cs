namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Domain.ValueObject;

/// <summary>
/// Pase de abordar emitido tras el check-in. [NC-1] id renombrado.
/// SQL: boarding_pass.
///
/// [IR-4] flight_seat_id FK → flight_seat (reemplazó seat_confirmed VARCHAR).
/// UNIQUE: check_in_id — un boarding pass por check-in.
///
/// gate_id: puede diferir del gate del vuelo (cambios de puerta de última hora).
/// boarding_group: grupo de embarque (ej. A, B, 1, 2), nullable.
///
/// check_in_id y flight_seat_id son la clave operacional — inmutables.
/// Update(): modifica gate_id y boarding_group (datos operativos mutables).
/// </summary>
public sealed class BoardingPassAggregate
{
    public BoardingPassId Id             { get; private set; }
    public int            CheckInId      { get; private set; }
    public int?           GateId         { get; private set; }
    public string?        BoardingGroup  { get; private set; }
    public int            FlightSeatId   { get; private set; }

    private BoardingPassAggregate()
    {
        Id = null!;
    }

    public BoardingPassAggregate(
        BoardingPassId id,
        int            checkInId,
        int?           gateId,
        string?        boardingGroup,
        int            flightSeatId)
    {
        if (checkInId <= 0)
            throw new ArgumentException(
                "CheckInId must be a positive integer.", nameof(checkInId));

        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException(
                "GateId must be a positive integer when provided.", nameof(gateId));

        if (flightSeatId <= 0)
            throw new ArgumentException(
                "FlightSeatId must be a positive integer.", nameof(flightSeatId));

        ValidateBoardingGroup(boardingGroup);

        Id            = id;
        CheckInId     = checkInId;
        GateId        = gateId;
        BoardingGroup = boardingGroup?.Trim();
        FlightSeatId  = flightSeatId;
    }

    /// <summary>
    /// Actualiza la puerta de embarque y/o el grupo de embarque.
    /// Ambos campos pueden cambiar en operaciones de última hora.
    /// check_in_id y flight_seat_id son inmutables.
    /// </summary>
    public void Update(int? gateId, string? boardingGroup)
    {
        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException(
                "GateId must be a positive integer when provided.", nameof(gateId));

        ValidateBoardingGroup(boardingGroup);

        GateId        = gateId;
        BoardingGroup = boardingGroup?.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateBoardingGroup(string? boardingGroup)
    {
        if (boardingGroup is not null && boardingGroup.Trim().Length > 10)
            throw new ArgumentException(
                "BoardingGroup cannot exceed 10 characters.", nameof(boardingGroup));
    }
}
