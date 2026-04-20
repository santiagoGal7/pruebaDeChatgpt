# Módulo: Payment
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment`  
**Raíz de archivos:** `src/Modules/Payment/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `payment_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `reservation_id` | `INT NULL FK` | `int?` | FK → `reservation`. XOR con `ticket_id` |
| `ticket_id` | `INT NULL FK` | `int?` | FK → `ticket`. XOR con `reservation_id` |
| `currency_id` | `INT NOT NULL FK` | `int` | FK → `currency` [TN-1] |
| `payment_date` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Momento del pago |
| `amount` | `DECIMAL(12,2) NOT NULL` | `decimal` | CHECK `>= 0` |
| `payment_status_id` | `INT NOT NULL FK` | `int` | FK → `payment_status` |
| `payment_method_id` | `INT NOT NULL FK` | `int` | FK → `payment_method` |
| `transaction_reference` | `VARCHAR(100) NULL` | `string?` | Referencia de la pasarela |
| `rejection_reason` | `VARCHAR(250) NULL` | `string?` | Motivo si el pago es rechazado |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**CHECKs del DDL (espejados en dominio):**
- `amount >= 0`
- **XOR exclusivo:** `(reservation_id IS NOT NULL AND ticket_id IS NULL) OR (reservation_id IS NULL AND ticket_id IS NOT NULL)` — un pago pertenece a una reserva O a un tiquete, nunca a ambos ni a ninguno.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Payment/Domain/valueObject/PaymentId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;

public sealed class PaymentId
{
    public int Value { get; }

    public PaymentId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("PaymentId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is PaymentId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Payment/Domain/aggregate/PaymentAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;

/// <summary>
/// Pago vinculado a una reserva O a un tiquete (XOR exclusivo).
/// SQL: payment. [TN-1] currency_id añadido para soporte multi-moneda.
///
/// Invariantes (espejo de los CHECKs del DDL):
///   1. amount >= 0 (chk_pay_amount).
///   2. XOR exclusivo (chk_pay_xor): reservation_id XOR ticket_id.
///      Un pago no puede estar vinculado a ambos ni a ninguno.
///
/// UpdateStatus(): única mutación válida — actualiza estado del pago,
/// transaction_reference y rejection_reason según el flujo de pago.
/// </summary>
public sealed class PaymentAggregate
{
    public PaymentId Id                   { get; private set; }
    public int?      ReservationId        { get; private set; }
    public int?      TicketId             { get; private set; }
    public int       CurrencyId           { get; private set; }
    public DateTime  PaymentDate          { get; private set; }
    public decimal   Amount               { get; private set; }
    public int       PaymentStatusId      { get; private set; }
    public int       PaymentMethodId      { get; private set; }
    public string?   TransactionReference { get; private set; }
    public string?   RejectionReason      { get; private set; }
    public DateTime  CreatedAt            { get; private set; }
    public DateTime? UpdatedAt            { get; private set; }

    private PaymentAggregate()
    {
        Id = null!;
    }

    public PaymentAggregate(
        PaymentId id,
        int?      reservationId,
        int?      ticketId,
        int       currencyId,
        DateTime  paymentDate,
        decimal   amount,
        int       paymentStatusId,
        int       paymentMethodId,
        string?   transactionReference,
        string?   rejectionReason,
        DateTime  createdAt,
        DateTime? updatedAt = null)
    {
        // XOR exclusivo: reservation_id XOR ticket_id
        ValidateXor(reservationId, ticketId);
        ValidateAmount(amount);
        ValidateForeignKeys(currencyId, paymentStatusId, paymentMethodId);
        ValidateTransactionReference(transactionReference);
        ValidateRejectionReason(rejectionReason);

        Id                   = id;
        ReservationId        = reservationId;
        TicketId             = ticketId;
        CurrencyId           = currencyId;
        PaymentDate          = paymentDate;
        Amount               = amount;
        PaymentStatusId      = paymentStatusId;
        PaymentMethodId      = paymentMethodId;
        TransactionReference = transactionReference?.Trim();
        RejectionReason      = rejectionReason?.Trim();
        CreatedAt            = createdAt;
        UpdatedAt            = updatedAt;
    }

    /// <summary>
    /// Actualiza el estado del pago junto con referencia y motivo de rechazo.
    /// ReservationId, TicketId, Amount, CurrencyId, PaymentDate y PaymentMethodId
    /// son inmutables tras la creación.
    /// </summary>
    public void UpdateStatus(
        int     paymentStatusId,
        string? transactionReference = null,
        string? rejectionReason      = null)
    {
        if (paymentStatusId <= 0)
            throw new ArgumentException(
                "PaymentStatusId must be a positive integer.", nameof(paymentStatusId));

        ValidateTransactionReference(transactionReference);
        ValidateRejectionReason(rejectionReason);

        PaymentStatusId      = paymentStatusId;
        TransactionReference = transactionReference?.Trim();
        RejectionReason      = rejectionReason?.Trim();
        UpdatedAt            = DateTime.UtcNow;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateXor(int? reservationId, int? ticketId)
    {
        bool hasReservation = reservationId.HasValue;
        bool hasTicket      = ticketId.HasValue;

        if (hasReservation && hasTicket)
            throw new ArgumentException(
                "A payment cannot be linked to both a reservation and a ticket (XOR constraint).");

        if (!hasReservation && !hasTicket)
            throw new ArgumentException(
                "A payment must be linked to either a reservation or a ticket (XOR constraint).");

        if (reservationId.HasValue && reservationId.Value <= 0)
            throw new ArgumentException(
                "ReservationId must be a positive integer.", nameof(reservationId));

        if (ticketId.HasValue && ticketId.Value <= 0)
            throw new ArgumentException(
                "TicketId must be a positive integer.", nameof(ticketId));
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount must be >= 0.", nameof(amount));
    }

    private static void ValidateForeignKeys(int currencyId, int paymentStatusId, int paymentMethodId)
    {
        if (currencyId <= 0)
            throw new ArgumentException("CurrencyId must be a positive integer.", nameof(currencyId));
        if (paymentStatusId <= 0)
            throw new ArgumentException("PaymentStatusId must be a positive integer.", nameof(paymentStatusId));
        if (paymentMethodId <= 0)
            throw new ArgumentException("PaymentMethodId must be a positive integer.", nameof(paymentMethodId));
    }

    private static void ValidateTransactionReference(string? reference)
    {
        if (reference is not null && reference.Trim().Length > 100)
            throw new ArgumentException(
                "TransactionReference cannot exceed 100 characters.", nameof(reference));
    }

    private static void ValidateRejectionReason(string? reason)
    {
        if (reason is not null && reason.Trim().Length > 250)
            throw new ArgumentException(
                "RejectionReason cannot exceed 250 characters.", nameof(reason));
    }
}
```

---

### RUTA: `src/Modules/Payment/Domain/Repositories/IPaymentRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;

public interface IPaymentRepository
{
    Task<PaymentAggregate?>             GetByIdAsync(PaymentId id,                         CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentAggregate>> GetAllAsync(                                        CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentAggregate>> GetByReservationAsync(int reservationId,            CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentAggregate>> GetByTicketAsync(int ticketId,                      CancellationToken cancellationToken = default);
    Task                                AddAsync(PaymentAggregate payment,                  CancellationToken cancellationToken = default);
    Task                                UpdateAsync(PaymentAggregate payment,               CancellationToken cancellationToken = default);
    Task                                DeleteAsync(PaymentId id,                           CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Payment/Application/Interfaces/IPaymentService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto?>             GetByIdAsync(int id,                                                                         CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetAllAsync(                                                                                 CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetByReservationAsync(int reservationId,                                                     CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetByTicketAsync(int ticketId,                                                               CancellationToken cancellationToken = default);
    Task<PaymentDto>              CreateAsync(CreatePaymentRequest request,                                                    CancellationToken cancellationToken = default);
    Task                          UpdateStatusAsync(int id, int paymentStatusId, string? transactionReference, string? rejectionReason, CancellationToken cancellationToken = default);
    Task                          DeleteAsync(int id,                                                                          CancellationToken cancellationToken = default);
}

public sealed record PaymentDto(
    int      Id,
    int?     ReservationId,
    int?     TicketId,
    int      CurrencyId,
    DateTime PaymentDate,
    decimal  Amount,
    int      PaymentStatusId,
    int      PaymentMethodId,
    string?  TransactionReference,
    string?  RejectionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreatePaymentRequest(
    int?    ReservationId,
    int?    TicketId,
    int     CurrencyId,
    decimal Amount,
    int     PaymentStatusId,
    int     PaymentMethodId,
    string? TransactionReference = null,
    string? RejectionReason      = null);
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/CreatePaymentUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreatePaymentUseCase
{
    private readonly IPaymentRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public CreatePaymentUseCase(IPaymentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentAggregate> ExecuteAsync(
        CreatePaymentRequest request,
        CancellationToken    cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // PaymentId(1) es placeholder; EF Core asigna el Id real al insertar.
        var payment = new PaymentAggregate(
            new PaymentId(1),
            request.ReservationId,
            request.TicketId,
            request.CurrencyId,
            now,
            request.Amount,
            request.PaymentStatusId,
            request.PaymentMethodId,
            request.TransactionReference,
            request.RejectionReason,
            now);

        await _repository.AddAsync(payment, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return payment;
    }
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/DeletePaymentUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeletePaymentUseCase
{
    private readonly IPaymentRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public DeletePaymentUseCase(IPaymentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new PaymentId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/GetAllPaymentsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;

public sealed class GetAllPaymentsUseCase
{
    private readonly IPaymentRepository _repository;

    public GetAllPaymentsUseCase(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/GetPaymentByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;

public sealed class GetPaymentByIdUseCase
{
    private readonly IPaymentRepository _repository;

    public GetPaymentByIdUseCase(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new PaymentId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/UpdatePaymentStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza el estado del pago. También puede actualizar la referencia de
/// transacción (al aprobar) y el motivo de rechazo (al rechazar).
/// ReservationId, TicketId, Amount, CurrencyId y PaymentMethodId son inmutables.
/// </summary>
public sealed class UpdatePaymentStatusUseCase
{
    private readonly IPaymentRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public UpdatePaymentStatusUseCase(IPaymentRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               paymentStatusId,
        string?           transactionReference,
        string?           rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _repository.GetByIdAsync(new PaymentId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Payment with id {id} was not found.");

        payment.UpdateStatus(paymentStatusId, transactionReference, rejectionReason);
        await _repository.UpdateAsync(payment, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/GetPaymentsByReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;

/// <summary>Obtiene todos los pagos asociados a una reserva.</summary>
public sealed class GetPaymentsByReservationUseCase
{
    private readonly IPaymentRepository _repository;

    public GetPaymentsByReservationUseCase(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentAggregate>> ExecuteAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByReservationAsync(reservationId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Payment/Application/UseCases/GetPaymentsByTicketUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;

/// <summary>Obtiene todos los pagos asociados a un tiquete.</summary>
public sealed class GetPaymentsByTicketUseCase
{
    private readonly IPaymentRepository _repository;

    public GetPaymentsByTicketUseCase(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentAggregate>> ExecuteAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByTicketAsync(ticketId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Payment/Application/Services/PaymentService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;

public sealed class PaymentService : IPaymentService
{
    private readonly CreatePaymentUseCase          _create;
    private readonly DeletePaymentUseCase          _delete;
    private readonly GetAllPaymentsUseCase         _getAll;
    private readonly GetPaymentByIdUseCase         _getById;
    private readonly UpdatePaymentStatusUseCase    _updateStatus;
    private readonly GetPaymentsByReservationUseCase _getByReservation;
    private readonly GetPaymentsByTicketUseCase    _getByTicket;

    public PaymentService(
        CreatePaymentUseCase           create,
        DeletePaymentUseCase           delete,
        GetAllPaymentsUseCase          getAll,
        GetPaymentByIdUseCase          getById,
        UpdatePaymentStatusUseCase     updateStatus,
        GetPaymentsByReservationUseCase getByReservation,
        GetPaymentsByTicketUseCase     getByTicket)
    {
        _create          = create;
        _delete          = delete;
        _getAll          = getAll;
        _getById         = getById;
        _updateStatus    = updateStatus;
        _getByReservation = getByReservation;
        _getByTicket     = getByTicket;
    }

    public async Task<PaymentDto> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken    cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(request, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<PaymentDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<PaymentDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateStatusAsync(
        int               id,
        int               paymentStatusId,
        string?           transactionReference,
        string?           rejectionReason,
        CancellationToken cancellationToken = default)
        => await _updateStatus.ExecuteAsync(id, paymentStatusId, transactionReference, rejectionReason, cancellationToken);

    public async Task<IEnumerable<PaymentDto>> GetByReservationAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByReservation.ExecuteAsync(reservationId, cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<PaymentDto>> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByTicket.ExecuteAsync(ticketId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static PaymentDto ToDto(PaymentAggregate agg)
        => new(
            agg.Id.Value,
            agg.ReservationId,
            agg.TicketId,
            agg.CurrencyId,
            agg.PaymentDate,
            agg.Amount,
            agg.PaymentStatusId,
            agg.PaymentMethodId,
            agg.TransactionReference,
            agg.RejectionReason,
            agg.CreatedAt,
            agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Payment/Infrastructure/entity/PaymentEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Infrastructure.Entity;

public sealed class PaymentEntity
{
    public int       Id                   { get; set; }
    public int?      ReservationId        { get; set; }
    public int?      TicketId             { get; set; }
    public int       CurrencyId           { get; set; }
    public DateTime  PaymentDate          { get; set; }
    public decimal   Amount               { get; set; }
    public int       PaymentStatusId      { get; set; }
    public int       PaymentMethodId      { get; set; }
    public string?   TransactionReference { get; set; }
    public string?   RejectionReason      { get; set; }
    public DateTime  CreatedAt            { get; set; }
    public DateTime? UpdatedAt            { get; set; }
}
```

---

### RUTA: `src/Modules/Payment/Infrastructure/entity/PaymentEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PaymentEntityConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("payment_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ReservationId)
               .HasColumnName("reservation_id")
               .IsRequired(false);

        builder.Property(e => e.TicketId)
               .HasColumnName("ticket_id")
               .IsRequired(false);

        builder.Property(e => e.CurrencyId)
               .HasColumnName("currency_id")
               .IsRequired();

        builder.Property(e => e.PaymentDate)
               .HasColumnName("payment_date")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Amount)
               .HasColumnName("amount")
               .IsRequired()
               .HasColumnType("decimal(12,2)");

        builder.Property(e => e.PaymentStatusId)
               .HasColumnName("payment_status_id")
               .IsRequired();

        builder.Property(e => e.PaymentMethodId)
               .HasColumnName("payment_method_id")
               .IsRequired();

        builder.Property(e => e.TransactionReference)
               .HasColumnName("transaction_reference")
               .IsRequired(false)
               .HasMaxLength(100);

        builder.Property(e => e.RejectionReason)
               .HasColumnName("rejection_reason")
               .IsRequired(false)
               .HasMaxLength(250);

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);
    }
}
```

---

### RUTA: `src/Modules/Payment/Infrastructure/repository/PaymentRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static PaymentAggregate ToDomain(PaymentEntity entity)
        => new(
            new PaymentId(entity.Id),
            entity.ReservationId,
            entity.TicketId,
            entity.CurrencyId,
            entity.PaymentDate,
            entity.Amount,
            entity.PaymentStatusId,
            entity.PaymentMethodId,
            entity.TransactionReference,
            entity.RejectionReason,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<PaymentAggregate?> GetByIdAsync(
        PaymentId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<PaymentAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Payments
            .AsNoTracking()
            .OrderByDescending(e => e.PaymentDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<PaymentAggregate>> GetByReservationAsync(
        int               reservationId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Payments
            .AsNoTracking()
            .Where(e => e.ReservationId == reservationId)
            .OrderByDescending(e => e.PaymentDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<PaymentAggregate>> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Payments
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.PaymentDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        PaymentAggregate  payment,
        CancellationToken cancellationToken = default)
    {
        var entity = new PaymentEntity
        {
            ReservationId        = payment.ReservationId,
            TicketId             = payment.TicketId,
            CurrencyId           = payment.CurrencyId,
            PaymentDate          = payment.PaymentDate,
            Amount               = payment.Amount,
            PaymentStatusId      = payment.PaymentStatusId,
            PaymentMethodId      = payment.PaymentMethodId,
            TransactionReference = payment.TransactionReference,
            RejectionReason      = payment.RejectionReason,
            CreatedAt            = payment.CreatedAt,
            UpdatedAt            = payment.UpdatedAt
        };
        await _context.Payments.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        PaymentAggregate  payment,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Payments
            .FirstOrDefaultAsync(e => e.Id == payment.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentEntity with id {payment.Id.Value} not found.");

        // Solo PaymentStatusId, TransactionReference, RejectionReason y UpdatedAt son mutables.
        // ReservationId, TicketId, Amount, CurrencyId, PaymentDate y PaymentMethodId son inmutables.
        entity.PaymentStatusId      = payment.PaymentStatusId;
        entity.TransactionReference = payment.TransactionReference;
        entity.RejectionReason      = payment.RejectionReason;
        entity.UpdatedAt            = payment.UpdatedAt;

        _context.Payments.Update(entity);
    }

    public async Task DeleteAsync(
        PaymentId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Payments
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentEntity with id {id.Value} not found.");

        _context.Payments.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Payment/UI/PaymentConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Payment.Application.Interfaces;

public sealed class PaymentConsoleUI
{
    private readonly IPaymentService _service;

    public PaymentConsoleUI(IPaymentService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== PAYMENT MODULE ==========");
            Console.WriteLine("1. List all payments");
            Console.WriteLine("2. Get payment by ID");
            Console.WriteLine("3. List by reservation");
            Console.WriteLine("4. List by ticket");
            Console.WriteLine("5. Register payment");
            Console.WriteLine("6. Update payment status");
            Console.WriteLine("7. Delete payment");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();             break;
                case "2": await GetByIdAsync();             break;
                case "3": await ListByReservationAsync();   break;
                case "4": await ListByTicketAsync();        break;
                case "5": await RegisterPaymentAsync();     break;
                case "6": await UpdateStatusAsync();        break;
                case "7": await DeleteAsync();              break;
                case "0": running = false;                  break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var payments = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Payments ---");
        foreach (var p in payments) PrintPayment(p);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter payment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByIdAsync(id);
        if (p is null) Console.WriteLine($"Payment with ID {id} not found.");
        else           PrintPayment(p);
    }

    private async Task ListByReservationAsync()
    {
        Console.Write("Enter Reservation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int resId))
        { Console.WriteLine("Invalid ID."); return; }

        var payments = await _service.GetByReservationAsync(resId);
        Console.WriteLine($"\n--- Payments for Reservation {resId} ---");
        foreach (var p in payments) PrintPayment(p);
    }

    private async Task ListByTicketAsync()
    {
        Console.Write("Enter Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int tickId))
        { Console.WriteLine("Invalid ID."); return; }

        var payments = await _service.GetByTicketAsync(tickId);
        Console.WriteLine($"\n--- Payments for Ticket {tickId} ---");
        foreach (var p in payments) PrintPayment(p);
    }

    private async Task RegisterPaymentAsync()
    {
        Console.WriteLine("Link to: 1) Reservation  2) Ticket");
        Console.Write("Select: ");
        var linkType = Console.ReadLine()?.Trim();

        int? reservationId = null;
        int? ticketId      = null;

        if (linkType == "1")
        {
            Console.Write("Reservation ID: ");
            if (!int.TryParse(Console.ReadLine(), out int rId)) { Console.WriteLine("Invalid."); return; }
            reservationId = rId;
        }
        else if (linkType == "2")
        {
            Console.Write("Ticket ID: ");
            if (!int.TryParse(Console.ReadLine(), out int tId)) { Console.WriteLine("Invalid."); return; }
            ticketId = tId;
        }
        else { Console.WriteLine("Invalid selection."); return; }

        Console.Write("Currency ID: ");
        if (!int.TryParse(Console.ReadLine(), out int currencyId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Amount (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal amount)) { Console.WriteLine("Invalid amount."); return; }

        Console.Write("Payment Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Payment Method ID: ");
        if (!int.TryParse(Console.ReadLine(), out int methodId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Transaction reference (optional): ");
        var refInput = Console.ReadLine()?.Trim();
        string? txRef = string.IsNullOrWhiteSpace(refInput) ? null : refInput;

        try
        {
            var request = new CreatePaymentRequest(
                reservationId, ticketId, currencyId, amount, statusId, methodId, txRef);
            var created = await _service.CreateAsync(request);
            Console.WriteLine($"Payment registered: [{created.Id}] Amount: {created.Amount:F2} | Status: {created.PaymentStatusId}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateStatusAsync()
    {
        Console.Write("Payment ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New Payment Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Transaction reference (optional): ");
        var refInput = Console.ReadLine()?.Trim();
        string? txRef = string.IsNullOrWhiteSpace(refInput) ? null : refInput;

        Console.Write("Rejection reason (optional): ");
        var rejInput = Console.ReadLine()?.Trim();
        string? rejection = string.IsNullOrWhiteSpace(rejInput) ? null : rejInput;

        try
        {
            await _service.UpdateStatusAsync(id, statusId, txRef, rejection);
            Console.WriteLine("Payment status updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Payment ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Payment deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintPayment(PaymentDto p)
    {
        var target = p.ReservationId.HasValue
            ? $"Reservation:{p.ReservationId}"
            : $"Ticket:{p.TicketId}";

        Console.WriteLine(
            $"  [{p.Id}] {target} | Currency:{p.CurrencyId} | " +
            $"Amount:{p.Amount:F2} | Status:{p.PaymentStatusId} | Method:{p.PaymentMethodId} | " +
            $"Date:{p.PaymentDate:yyyy-MM-dd HH:mm}" +
            (p.TransactionReference is not null ? $" | Ref:{p.TransactionReference}" : string.Empty));
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Payment Module ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<CreatePaymentUseCase>();
builder.Services.AddScoped<DeletePaymentUseCase>();
builder.Services.AddScoped<GetAllPaymentsUseCase>();
builder.Services.AddScoped<GetPaymentByIdUseCase>();
builder.Services.AddScoped<UpdatePaymentStatusUseCase>();
builder.Services.AddScoped<GetPaymentsByReservationUseCase>();
builder.Services.AddScoped<GetPaymentsByTicketUseCase>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<PaymentConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Payment/
├── Application/
│   ├── Interfaces/
│   │   └── IPaymentService.cs
│   ├── Services/
│   │   └── PaymentService.cs
│   └── UseCases/
│       ├── CreatePaymentUseCase.cs
│       ├── DeletePaymentUseCase.cs
│       ├── GetAllPaymentsUseCase.cs
│       ├── GetPaymentByIdUseCase.cs
│       ├── GetPaymentsByReservationUseCase.cs
│       ├── GetPaymentsByTicketUseCase.cs
│       └── UpdatePaymentStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── PaymentAggregate.cs
│   ├── Repositories/
│   │   └── IPaymentRepository.cs
│   └── valueObject/
│       └── PaymentId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── PaymentEntity.cs
│   │   └── PaymentEntityConfiguration.cs
│   └── repository/
│       └── PaymentRepository.cs
└── UI/
    └── PaymentConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Payment_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
