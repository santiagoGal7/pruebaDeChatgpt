namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

/// <summary>
/// Franquicia de equipaje incluida según la combinación clase de cabina + tipo de tarifa.
/// SQL: baggage_allowance. [NC-9] id renombrado a baggage_allowance_id.
///
/// 4NF: (cabin_class_id, fare_type_id) → carry_on_pieces, carry_on_kg,
///      checked_pieces, checked_kg. Sin MVD independientes.
/// UNIQUE: (cabin_class_id, fare_type_id).
///
/// Update(): modifica los límites de equipaje cuando cambia la política.
/// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class BaggageAllowanceAggregate
{
    public BaggageAllowanceId Id             { get; private set; }
    public int                CabinClassId   { get; private set; }
    public int                FareTypeId     { get; private set; }
    public int                CarryOnPieces  { get; private set; }
    public decimal            CarryOnKg      { get; private set; }
    public int                CheckedPieces  { get; private set; }
    public decimal            CheckedKg      { get; private set; }

    private BaggageAllowanceAggregate()
    {
        Id = null!;
    }

    public BaggageAllowanceAggregate(
        BaggageAllowanceId id,
        int                cabinClassId,
        int                fareTypeId,
        int                carryOnPieces,
        decimal            carryOnKg,
        int                checkedPieces,
        decimal            checkedKg)
    {
        if (cabinClassId <= 0)
            throw new ArgumentException("CabinClassId must be a positive integer.", nameof(cabinClassId));

        if (fareTypeId <= 0)
            throw new ArgumentException("FareTypeId must be a positive integer.", nameof(fareTypeId));

        ValidateLimits(carryOnPieces, carryOnKg, checkedPieces, checkedKg);

        Id            = id;
        CabinClassId  = cabinClassId;
        FareTypeId    = fareTypeId;
        CarryOnPieces = carryOnPieces;
        CarryOnKg     = carryOnKg;
        CheckedPieces = checkedPieces;
        CheckedKg     = checkedKg;
    }

    /// <summary>
    /// Actualiza los límites de la franquicia de equipaje.
    /// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
    /// </summary>
    public void Update(int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg)
    {
        ValidateLimits(carryOnPieces, carryOnKg, checkedPieces, checkedKg);

        CarryOnPieces = carryOnPieces;
        CarryOnKg     = carryOnKg;
        CheckedPieces = checkedPieces;
        CheckedKg     = checkedKg;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateLimits(
        int     carryOnPieces,
        decimal carryOnKg,
        int     checkedPieces,
        decimal checkedKg)
    {
        if (carryOnPieces < 0)
            throw new ArgumentException("CarryOnPieces must be >= 0.", nameof(carryOnPieces));

        if (carryOnKg < 0)
            throw new ArgumentException("CarryOnKg must be >= 0.", nameof(carryOnKg));

        if (checkedPieces < 0)
            throw new ArgumentException("CheckedPieces must be >= 0.", nameof(checkedPieces));

        if (checkedKg < 0)
            throw new ArgumentException("CheckedKg must be >= 0.", nameof(checkedKg));
    }
}
