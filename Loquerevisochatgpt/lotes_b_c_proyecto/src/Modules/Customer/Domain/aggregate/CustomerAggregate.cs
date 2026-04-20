namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;

using System.Text.RegularExpressions;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

/// <summary>
/// Rol cliente: la persona que compra o realiza reservas.
/// SQL: customer. person_id UNIQUE — una persona es cliente como máximo una vez.
///
/// Invariantes:
///   - email nullable pero, si se provee, debe respetar el formato
///     '^[^@]+@[^@]+\.[^@]+$' (espejo del CHECK SQL).
///   - email UNIQUE en la BD — la unicidad se garantiza a nivel de base de datos.
///   - phone nullable, máximo 30 caracteres.
///   - El email se normaliza a minúsculas para evitar duplicados por capitalización.
/// </summary>
public sealed class CustomerAggregate
{
    // Espejo del CHECK SQL: ^[^@]+@[^@]+\.[^@]+$
    private static readonly Regex EmailRegex =
        new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CustomerId Id        { get; private set; }
    public int        PersonId  { get; private set; }
    public string?    Phone     { get; private set; }
    public string?    Email     { get; private set; }
    public DateTime   CreatedAt { get; private set; }
    public DateTime?  UpdatedAt { get; private set; }

    private CustomerAggregate()
    {
        Id = null!;
    }

    public CustomerAggregate(
        CustomerId id,
        int        personId,
        string?    phone,
        string?    email,
        DateTime   createdAt,
        DateTime?  updatedAt = null)
    {
        if (personId <= 0)
            throw new ArgumentException("PersonId must be a positive integer.", nameof(personId));

        ValidatePhone(phone);
        ValidateEmail(email);

        Id        = id;
        PersonId  = personId;
        Phone     = phone?.Trim();
        Email     = email?.Trim().ToLowerInvariant();
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Actualiza teléfono y email del cliente.
    /// PersonId no es modificable: identifica a quién es el cliente.
    /// </summary>
    public void UpdateContact(string? phone, string? email)
    {
        ValidatePhone(phone);
        ValidateEmail(email);

        Phone     = phone?.Trim();
        Email     = email?.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidatePhone(string? phone)
    {
        if (phone is not null && phone.Trim().Length > 30)
            throw new ArgumentException(
                "Phone cannot exceed 30 characters.", nameof(phone));
    }

    private static void ValidateEmail(string? email)
    {
        if (email is null)
            return;

        var trimmed = email.Trim();

        if (trimmed.Length > 120)
            throw new ArgumentException(
                "Email cannot exceed 120 characters.", nameof(email));

        if (!EmailRegex.IsMatch(trimmed))
            throw new ArgumentException(
                "Email format is invalid. Expected: local@domain.tld", nameof(email));
    }
}
