namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

/// <summary>
/// Tipo de equipaje adicional cobrable (por encima de la franquicia incluida).
/// SQL: baggage_type.
///
/// Invariantes:
///   - name: máximo 80 caracteres, UNIQUE, normalizado a mayúsculas.
///   - max_weight_kg: peso máximo para este tipo de equipaje.
///   - extra_fee >= 0 (espejo del chk_bt_fee).
///
/// Update(): modifica nombre, peso máximo y tarifa.
/// </summary>
public sealed class BaggageTypeAggregate
{
    public BaggageTypeId Id           { get; private set; }
    public string        Name         { get; private set; }
    public decimal       MaxWeightKg  { get; private set; }
    public decimal       ExtraFee     { get; private set; }

    private BaggageTypeAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public BaggageTypeAggregate(
        BaggageTypeId id,
        string        name,
        decimal       maxWeightKg,
        decimal       extraFee)
    {
        ValidateName(name);
        ValidateMaxWeightKg(maxWeightKg);
        ValidateExtraFee(extraFee);

        Id          = id;
        Name        = name.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        ExtraFee    = extraFee;
    }

    public void Update(string name, decimal maxWeightKg, decimal extraFee)
    {
        ValidateName(name);
        ValidateMaxWeightKg(maxWeightKg);
        ValidateExtraFee(extraFee);

        Name        = name.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        ExtraFee    = extraFee;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("BaggageType name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "BaggageType name cannot exceed 80 characters.", nameof(name));
    }

    private static void ValidateMaxWeightKg(decimal maxWeightKg)
    {
        if (maxWeightKg <= 0)
            throw new ArgumentException(
                "MaxWeightKg must be a positive value.", nameof(maxWeightKg));
    }

    private static void ValidateExtraFee(decimal extraFee)
    {
        if (extraFee < 0)
            throw new ArgumentException(
                "ExtraFee must be >= 0. [chk_bt_fee]", nameof(extraFee));
    }
}
