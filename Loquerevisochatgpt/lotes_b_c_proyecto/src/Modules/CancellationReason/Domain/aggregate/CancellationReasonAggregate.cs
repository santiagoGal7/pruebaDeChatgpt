namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

/// <summary>
/// Catálogo de razones de cancelación de vuelos.
/// SQL: cancellation_reason.
///
/// Valores esperados: WEATHER, TECHNICAL, COMMERCIAL, FORCE_MAJEURE.
/// El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.
/// Nota: a diferencia de delay_reason, NO tiene campo category en el DDL.
/// </summary>
public sealed class CancellationReasonAggregate
{
    public CancellationReasonId Id   { get; private set; }
    public string               Name { get; private set; }

    private CancellationReasonAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public CancellationReasonAggregate(CancellationReasonId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CancellationReason name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "CancellationReason name cannot exceed 80 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("CancellationReason name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 80)
            throw new ArgumentException(
                "CancellationReason name cannot exceed 80 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
