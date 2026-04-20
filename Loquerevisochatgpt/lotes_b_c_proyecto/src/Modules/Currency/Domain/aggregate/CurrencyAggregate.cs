namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

/// <summary>
/// Moneda ISO 4217 para pagos multi-moneda. [TN-1]
/// SQL: currency.
///
/// Invariantes:
///   - iso_code: exactamente 3 caracteres, normalizado a MAYÚSCULAS (ISO 4217).
///   - name: máximo 80 caracteres, único.
///   - symbol: máximo 5 caracteres.
/// </summary>
public sealed class CurrencyAggregate
{
    public CurrencyId Id      { get; private set; }
    public string     IsoCode { get; private set; }
    public string     Name    { get; private set; }
    public string     Symbol  { get; private set; }

    private CurrencyAggregate()
    {
        Id      = null!;
        IsoCode = null!;
        Name    = null!;
        Symbol  = null!;
    }

    public CurrencyAggregate(CurrencyId id, string isoCode, string name, string symbol)
    {
        ValidateIsoCode(isoCode);
        ValidateName(name);
        ValidateSymbol(symbol);

        Id      = id;
        IsoCode = isoCode.Trim().ToUpperInvariant();
        Name    = name.Trim();
        Symbol  = symbol.Trim();
    }

    public void Update(string isoCode, string name, string symbol)
    {
        ValidateIsoCode(isoCode);
        ValidateName(name);
        ValidateSymbol(symbol);

        IsoCode = isoCode.Trim().ToUpperInvariant();
        Name    = name.Trim();
        Symbol  = symbol.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateIsoCode(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
            throw new ArgumentException("IsoCode cannot be empty.", nameof(isoCode));

        if (isoCode.Trim().Length != 3)
            throw new ArgumentException(
                "IsoCode must be exactly 3 characters (ISO 4217).", nameof(isoCode));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "Currency name cannot exceed 80 characters.", nameof(name));
    }

    private static void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Currency symbol cannot be empty.", nameof(symbol));

        if (symbol.Trim().Length > 5)
            throw new ArgumentException(
                "Currency symbol cannot exceed 5 characters.", nameof(symbol));
    }
}
