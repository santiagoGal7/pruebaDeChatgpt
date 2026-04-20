# Módulo: Refund
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund`  
**Raíz de archivos:** `src/Modules/Refund/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `refund_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `payment_id` | `INT NOT NULL FK` | `int` | FK → `payment` |
| `refund_status_id` | `INT NOT NULL FK` | `int` | FK → `refund_status` |
| `amount` | `DECIMAL(12,2) NOT NULL` | `decimal` | CHECK `>= 0` — espejado en dominio |
| `requested_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `processed_at` | `DATETIME NULL` | `DateTime?` | Se establece al procesar el reembolso |
| `reason` | `VARCHAR(250) NULL` | `string?` | Motivo del reembolso, nullable |

**CHECK:** `amount >= 0` — espejado en el constructor y `AdjustAmount()`.  
`UpdateStatus()`: única mutación válida — actualiza estado, `processed_at` y opcionalmente `reason`.  
`payment_id`, `amount` y `requested_at` son inmutables tras crear el reembolso.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Refund/Domain/valueObject/RefundId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;

public sealed class RefundId
{
    public int Value { get; }

    public RefundId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("RefundId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is RefundId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Refund/Domain/aggregate/RefundAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;

/// <summary>
/// Solicitud de reembolso de un pago.
/// SQL: refund.
///
/// Invariantes:
///   - amount >= 0 (espejo del chk_refund_amount).
///   - payment_id, amount y requested_at son inmutables tras la creación.
///
/// Ciclo de vida típico: PENDING → APPROVED → PROCESSED
///                        PENDING → REJECTED
///
/// UpdateStatus(): única mutación válida.
///   - Registra processed_at cuando el estado indica que fue procesado/rechazado.
///   - reason: puede actualizarse al cambiar el estado.
/// </summary>
public sealed class RefundAggregate
{
    public RefundId  Id              { get; private set; }
    public int       PaymentId       { get; private set; }
    public int       RefundStatusId  { get; private set; }
    public decimal   Amount          { get; private set; }
    public DateTime  RequestedAt     { get; private set; }
    public DateTime? ProcessedAt     { get; private set; }
    public string?   Reason          { get; private set; }

    private RefundAggregate()
    {
        Id = null!;
    }

    public RefundAggregate(
        RefundId  id,
        int       paymentId,
        int       refundStatusId,
        decimal   amount,
        DateTime  requestedAt,
        DateTime? processedAt = null,
        string?   reason      = null)
    {
        if (paymentId <= 0)
            throw new ArgumentException(
                "PaymentId must be a positive integer.", nameof(paymentId));

        if (refundStatusId <= 0)
            throw new ArgumentException(
                "RefundStatusId must be a positive integer.", nameof(refundStatusId));

        ValidateAmount(amount);
        ValidateReason(reason);

        if (processedAt.HasValue && processedAt.Value < requestedAt)
            throw new ArgumentException(
                "processed_at cannot be earlier than requested_at.", nameof(processedAt));

        Id             = id;
        PaymentId      = paymentId;
        RefundStatusId = refundStatusId;
        Amount         = amount;
        RequestedAt    = requestedAt;
        ProcessedAt    = processedAt;
        Reason         = reason?.Trim();
    }

    /// <summary>
    /// Actualiza el estado del reembolso.
    /// Opcionalmente registra processed_at (cuando se aprueba, rechaza o procesa)
    /// y actualiza el motivo.
    /// payment_id, amount y requested_at son inmutables.
    /// </summary>
    public void UpdateStatus(int refundStatusId, DateTime? processedAt = null, string? reason = null)
    {
        if (refundStatusId <= 0)
            throw new ArgumentException(
                "RefundStatusId must be a positive integer.", nameof(refundStatusId));

        if (processedAt.HasValue && processedAt.Value < RequestedAt)
            throw new ArgumentException(
                "processed_at cannot be earlier than requested_at.", nameof(processedAt));

        ValidateReason(reason);

        RefundStatusId = refundStatusId;
        ProcessedAt    = processedAt ?? ProcessedAt;
        Reason         = reason?.Trim() ?? Reason;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount must be >= 0.", nameof(amount));
    }

    private static void ValidateReason(string? reason)
    {
        if (reason is not null && reason.Trim().Length > 250)
            throw new ArgumentException(
                "Reason cannot exceed 250 characters.", nameof(reason));
    }
}
```

---

### RUTA: `src/Modules/Refund/Domain/Repositories/IRefundRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;

public interface IRefundRepository
{
    Task<RefundAggregate?>             GetByIdAsync(RefundId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundAggregate>> GetAllAsync(                                    CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundAggregate>> GetByPaymentAsync(int paymentId,               CancellationToken cancellationToken = default);
    Task                               AddAsync(RefundAggregate refund,               CancellationToken cancellationToken = default);
    Task                               UpdateAsync(RefundAggregate refund,            CancellationToken cancellationToken = default);
    Task                               DeleteAsync(RefundId id,                       CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Refund/Application/Interfaces/IRefundService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.Interfaces;

public interface IRefundService
{
    Task<RefundDto?>             GetByIdAsync(int id,                                                                     CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundDto>> GetAllAsync(                                                                             CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundDto>> GetByPaymentAsync(int paymentId,                                                         CancellationToken cancellationToken = default);
    Task<RefundDto>              CreateAsync(int paymentId, int refundStatusId, decimal amount, string? reason,           CancellationToken cancellationToken = default);
    Task                         UpdateStatusAsync(int id, int refundStatusId, DateTime? processedAt, string? reason,    CancellationToken cancellationToken = default);
    Task                         DeleteAsync(int id,                                                                      CancellationToken cancellationToken = default);
}

public sealed record RefundDto(
    int      Id,
    int      PaymentId,
    int      RefundStatusId,
    decimal  Amount,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    string?  Reason);
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/CreateRefundUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateRefundUseCase
{
    private readonly IRefundRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public CreateRefundUseCase(IRefundRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefundAggregate> ExecuteAsync(
        int               paymentId,
        int               refundStatusId,
        decimal           amount,
        string?           reason,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // RefundId(1) es placeholder; EF Core asigna el Id real al insertar.
        var refund = new RefundAggregate(
            new RefundId(1),
            paymentId,
            refundStatusId,
            amount,
            now,
            processedAt: null,
            reason: reason);

        await _repository.AddAsync(refund, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return refund;
    }
}
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/DeleteRefundUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteRefundUseCase
{
    private readonly IRefundRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public DeleteRefundUseCase(IRefundRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new RefundId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/GetAllRefundsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;

public sealed class GetAllRefundsUseCase
{
    private readonly IRefundRepository _repository;

    public GetAllRefundsUseCase(IRefundRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RefundAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/GetRefundByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;

public sealed class GetRefundByIdUseCase
{
    private readonly IRefundRepository _repository;

    public GetRefundByIdUseCase(IRefundRepository repository)
    {
        _repository = repository;
    }

    public async Task<RefundAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new RefundId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/UpdateRefundStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza el estado del reembolso y opcionalmente registra
/// la fecha de procesamiento y el motivo.
/// payment_id, amount y requested_at son inmutables.
/// </summary>
public sealed class UpdateRefundStatusUseCase
{
    private readonly IRefundRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public UpdateRefundStatusUseCase(IRefundRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               refundStatusId,
        DateTime?         processedAt,
        string?           reason,
        CancellationToken cancellationToken = default)
    {
        var refund = await _repository.GetByIdAsync(new RefundId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Refund with id {id} was not found.");

        refund.UpdateStatus(refundStatusId, processedAt, reason);
        await _repository.UpdateAsync(refund, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Refund/Application/UseCases/GetRefundsByPaymentUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;

/// <summary>Obtiene todos los reembolsos asociados a un pago.</summary>
public sealed class GetRefundsByPaymentUseCase
{
    private readonly IRefundRepository _repository;

    public GetRefundsByPaymentUseCase(IRefundRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RefundAggregate>> ExecuteAsync(
        int               paymentId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByPaymentAsync(paymentId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Refund/Application/Services/RefundService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;

public sealed class RefundService : IRefundService
{
    private readonly CreateRefundUseCase        _create;
    private readonly DeleteRefundUseCase        _delete;
    private readonly GetAllRefundsUseCase       _getAll;
    private readonly GetRefundByIdUseCase       _getById;
    private readonly UpdateRefundStatusUseCase  _updateStatus;
    private readonly GetRefundsByPaymentUseCase _getByPayment;

    public RefundService(
        CreateRefundUseCase        create,
        DeleteRefundUseCase        delete,
        GetAllRefundsUseCase       getAll,
        GetRefundByIdUseCase       getById,
        UpdateRefundStatusUseCase  updateStatus,
        GetRefundsByPaymentUseCase getByPayment)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _updateStatus = updateStatus;
        _getByPayment = getByPayment;
    }

    public async Task<RefundDto> CreateAsync(
        int               paymentId,
        int               refundStatusId,
        decimal           amount,
        string?           reason,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            paymentId, refundStatusId, amount, reason, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<RefundDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<RefundDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateStatusAsync(
        int               id,
        int               refundStatusId,
        DateTime?         processedAt,
        string?           reason,
        CancellationToken cancellationToken = default)
        => await _updateStatus.ExecuteAsync(id, refundStatusId, processedAt, reason, cancellationToken);

    public async Task<IEnumerable<RefundDto>> GetByPaymentAsync(
        int               paymentId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByPayment.ExecuteAsync(paymentId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static RefundDto ToDto(RefundAggregate agg)
        => new(
            agg.Id.Value,
            agg.PaymentId,
            agg.RefundStatusId,
            agg.Amount,
            agg.RequestedAt,
            agg.ProcessedAt,
            agg.Reason);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Refund/Infrastructure/entity/RefundEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Infrastructure.Entity;

public sealed class RefundEntity
{
    public int       Id             { get; set; }
    public int       PaymentId      { get; set; }
    public int       RefundStatusId { get; set; }
    public decimal   Amount         { get; set; }
    public DateTime  RequestedAt    { get; set; }
    public DateTime? ProcessedAt    { get; set; }
    public string?   Reason         { get; set; }
}
```

---

### RUTA: `src/Modules/Refund/Infrastructure/entity/RefundEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RefundEntityConfiguration : IEntityTypeConfiguration<RefundEntity>
{
    public void Configure(EntityTypeBuilder<RefundEntity> builder)
    {
        builder.ToTable("refund");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("refund_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.PaymentId)
               .HasColumnName("payment_id")
               .IsRequired();

        builder.Property(e => e.RefundStatusId)
               .HasColumnName("refund_status_id")
               .IsRequired();

        builder.Property(e => e.Amount)
               .HasColumnName("amount")
               .IsRequired()
               .HasColumnType("decimal(12,2)");

        builder.Property(e => e.RequestedAt)
               .HasColumnName("requested_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ProcessedAt)
               .HasColumnName("processed_at")
               .IsRequired(false);

        builder.Property(e => e.Reason)
               .HasColumnName("reason")
               .IsRequired(false)
               .HasMaxLength(250);
    }
}
```

---

### RUTA: `src/Modules/Refund/Infrastructure/repository/RefundRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class RefundRepository : IRefundRepository
{
    private readonly AppDbContext _context;

    public RefundRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static RefundAggregate ToDomain(RefundEntity entity)
        => new(
            new RefundId(entity.Id),
            entity.PaymentId,
            entity.RefundStatusId,
            entity.Amount,
            entity.RequestedAt,
            entity.ProcessedAt,
            entity.Reason);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<RefundAggregate?> GetByIdAsync(
        RefundId          id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Refunds
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<RefundAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Refunds
            .AsNoTracking()
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<RefundAggregate>> GetByPaymentAsync(
        int               paymentId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Refunds
            .AsNoTracking()
            .Where(e => e.PaymentId == paymentId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        RefundAggregate   refund,
        CancellationToken cancellationToken = default)
    {
        var entity = new RefundEntity
        {
            PaymentId      = refund.PaymentId,
            RefundStatusId = refund.RefundStatusId,
            Amount         = refund.Amount,
            RequestedAt    = refund.RequestedAt,
            ProcessedAt    = refund.ProcessedAt,
            Reason         = refund.Reason
        };
        await _context.Refunds.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        RefundAggregate   refund,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Refunds
            .FirstOrDefaultAsync(e => e.Id == refund.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RefundEntity with id {refund.Id.Value} not found.");

        // Solo RefundStatusId, ProcessedAt y Reason son mutables.
        // PaymentId, Amount y RequestedAt son inmutables.
        entity.RefundStatusId = refund.RefundStatusId;
        entity.ProcessedAt    = refund.ProcessedAt;
        entity.Reason         = refund.Reason;

        _context.Refunds.Update(entity);
    }

    public async Task DeleteAsync(
        RefundId          id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Refunds
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RefundEntity with id {id.Value} not found.");

        _context.Refunds.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Refund/UI/RefundConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Refund.Application.Interfaces;

public sealed class RefundConsoleUI
{
    private readonly IRefundService _service;

    public RefundConsoleUI(IRefundService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== REFUND MODULE ==========");
            Console.WriteLine("1. List all refunds");
            Console.WriteLine("2. Get refund by ID");
            Console.WriteLine("3. List refunds by payment");
            Console.WriteLine("4. Request refund");
            Console.WriteLine("5. Update refund status");
            Console.WriteLine("6. Delete refund");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByPaymentAsync();    break;
                case "4": await RequestRefundAsync();    break;
                case "5": await UpdateStatusAsync();     break;
                case "6": await DeleteAsync();           break;
                case "0": running = false;               break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var refunds = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Refunds ---");
        foreach (var r in refunds) PrintRefund(r);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter refund ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var r = await _service.GetByIdAsync(id);
        if (r is null) Console.WriteLine($"Refund with ID {id} not found.");
        else           PrintRefund(r);
    }

    private async Task ListByPaymentAsync()
    {
        Console.Write("Enter Payment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int paymentId))
        { Console.WriteLine("Invalid ID."); return; }

        var refunds = await _service.GetByPaymentAsync(paymentId);
        Console.WriteLine($"\n--- Refunds for Payment {paymentId} ---");
        foreach (var r in refunds) PrintRefund(r);
    }

    private async Task RequestRefundAsync()
    {
        Console.Write("Payment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int paymentId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Initial Refund Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Amount to refund (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal amount))
        { Console.WriteLine("Invalid amount."); return; }

        Console.Write("Reason (optional): ");
        var reasonInput = Console.ReadLine()?.Trim();
        string? reason = string.IsNullOrWhiteSpace(reasonInput) ? null : reasonInput;

        try
        {
            var created = await _service.CreateAsync(paymentId, statusId, amount, reason);
            Console.WriteLine(
                $"Refund requested: [{created.Id}] Payment:{created.PaymentId} | " +
                $"Amount:{created.Amount:F2} | Status:{created.RefundStatusId} | " +
                $"Requested:{created.RequestedAt:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateStatusAsync()
    {
        Console.Write("Refund ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New Refund Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Processed at (yyyy-MM-dd HH:mm, optional): ");
        var procInput = Console.ReadLine()?.Trim();
        DateTime? processedAt = DateTime.TryParse(procInput, out var parsedDt) ? parsedDt : null;

        Console.Write("Reason (optional, Enter to keep current): ");
        var reasonInput = Console.ReadLine()?.Trim();
        string? reason = string.IsNullOrWhiteSpace(reasonInput) ? null : reasonInput;

        try
        {
            await _service.UpdateStatusAsync(id, statusId, processedAt, reason);
            Console.WriteLine("Refund status updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Refund ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Refund deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintRefund(RefundDto r)
        => Console.WriteLine(
            $"  [{r.Id}] Payment:{r.PaymentId} | Status:{r.RefundStatusId} | " +
            $"Amount:{r.Amount:F2} | Requested:{r.RequestedAt:yyyy-MM-dd HH:mm}" +
            (r.ProcessedAt.HasValue ? $" | Processed:{r.ProcessedAt:yyyy-MM-dd HH:mm}" : string.Empty) +
            (r.Reason is not null ? $" | Reason:{r.Reason}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Refund Module ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<CreateRefundUseCase>();
builder.Services.AddScoped<DeleteRefundUseCase>();
builder.Services.AddScoped<GetAllRefundsUseCase>();
builder.Services.AddScoped<GetRefundByIdUseCase>();
builder.Services.AddScoped<UpdateRefundStatusUseCase>();
builder.Services.AddScoped<GetRefundsByPaymentUseCase>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<RefundConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Refund/
├── Application/
│   ├── Interfaces/
│   │   └── IRefundService.cs
│   ├── Services/
│   │   └── RefundService.cs
│   └── UseCases/
│       ├── CreateRefundUseCase.cs
│       ├── DeleteRefundUseCase.cs
│       ├── GetAllRefundsUseCase.cs
│       ├── GetRefundByIdUseCase.cs
│       ├── GetRefundsByPaymentUseCase.cs
│       └── UpdateRefundStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── RefundAggregate.cs
│   ├── Repositories/
│   │   └── IRefundRepository.cs
│   └── valueObject/
│       └── RefundId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── RefundEntity.cs
│   │   └── RefundEntityConfiguration.cs
│   └── repository/
│       └── RefundRepository.cs
└── UI/
    └── RefundConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Refund_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
