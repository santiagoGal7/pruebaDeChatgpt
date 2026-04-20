# Módulo: Reservation
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation`  
**Raíz de archivos:** `src/Modules/Reservation/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `reservation_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `reservation_code` | `VARCHAR(20) NOT NULL UNIQUE` | `string` | Código alfanumérico de negocio |
| `customer_id` | `INT NOT NULL FK` | `int` | FK → `customer` |
| `scheduled_flight_id` | `INT NOT NULL FK` | `int` | FK → `scheduled_flight` |
| `reservation_date` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Momento en que se crea la reserva |
| `reservation_status_id` | `INT NOT NULL FK` | `int` | FK → `reservation_status` |
| `confirmed_at` | `DATETIME NULL` | `DateTime?` | Seteado al confirmar |
| `cancelled_at` | `DATETIME NULL` | `DateTime?` | Seteado al cancelar |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**CHECKs del DDL (espejados en el dominio):**
- `confirmed_at IS NULL OR confirmed_at >= reservation_date`
- `cancelled_at IS NULL OR cancelled_at >= reservation_date`
- `confirmed_at IS NULL OR cancelled_at IS NULL` (exclusión mutua)

**UNIQUE:** `reservation_code`

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Reservation/Domain/valueObject/ReservationId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;

public sealed class ReservationId
{
    public int Value { get; }

    public ReservationId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("ReservationId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is ReservationId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Reservation/Domain/aggregate/ReservationAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;

/// <summary>
/// Reserva de un cliente para un vuelo concreto.
/// SQL: reservation.
///
/// Invariantes (espejo de los CHECKs del DDL):
///   1. confirmed_at >= reservation_date  (si no es null)
///   2. cancelled_at >= reservation_date  (si no es null)
///   3. confirmed_at y cancelled_at son mutuamente excluyentes (CHECK chk_mutual_excl)
///
/// Ciclo de vida: PENDING → CONFIRMED o PENDING → CANCELLED.
/// Una reserva confirmada no puede cancelarse y viceversa
/// (exclusión mutua garantizada en dominio y en BD).
///
/// reservation_code: código único de negocio, normalizado a mayúsculas.
/// </summary>
public sealed class ReservationAggregate
{
    public ReservationId Id                  { get; private set; }
    public string        ReservationCode     { get; private set; }
    public int           CustomerId          { get; private set; }
    public int           ScheduledFlightId   { get; private set; }
    public DateTime      ReservationDate     { get; private set; }
    public int           ReservationStatusId { get; private set; }
    public DateTime?     ConfirmedAt         { get; private set; }
    public DateTime?     CancelledAt         { get; private set; }
    public DateTime      CreatedAt           { get; private set; }
    public DateTime?     UpdatedAt           { get; private set; }

    private ReservationAggregate()
    {
        Id              = null!;
        ReservationCode = null!;
    }

    public ReservationAggregate(
        ReservationId id,
        string        reservationCode,
        int           customerId,
        int           scheduledFlightId,
        DateTime      reservationDate,
        int           reservationStatusId,
        DateTime?     confirmedAt,
        DateTime?     cancelledAt,
        DateTime      createdAt,
        DateTime?     updatedAt = null)
    {
        ValidateCode(reservationCode);

        if (customerId <= 0)
            throw new ArgumentException("CustomerId must be a positive integer.", nameof(customerId));

        if (scheduledFlightId <= 0)
            throw new ArgumentException("ScheduledFlightId must be a positive integer.", nameof(scheduledFlightId));

        if (reservationStatusId <= 0)
            throw new ArgumentException("ReservationStatusId must be a positive integer.", nameof(reservationStatusId));

        ValidateTimestamps(reservationDate, confirmedAt, cancelledAt);

        Id                  = id;
        ReservationCode     = reservationCode.Trim().ToUpperInvariant();
        CustomerId          = customerId;
        ScheduledFlightId   = scheduledFlightId;
        ReservationDate     = reservationDate;
        ReservationStatusId = reservationStatusId;
        ConfirmedAt         = confirmedAt;
        CancelledAt         = cancelledAt;
        CreatedAt           = createdAt;
        UpdatedAt           = updatedAt;
    }

    /// <summary>
    /// Confirma la reserva.
    /// Precondición: no debe estar ya cancelada (exclusión mutua).
    /// Establece confirmed_at = now y actualiza el status al ID proporcionado.
    /// </summary>
    public void Confirm(int confirmedStatusId)
    {
        if (CancelledAt.HasValue)
            throw new InvalidOperationException(
                "Cannot confirm a reservation that has already been cancelled.");

        if (ConfirmedAt.HasValue)
            throw new InvalidOperationException(
                "Reservation is already confirmed.");

        if (confirmedStatusId <= 0)
            throw new ArgumentException(
                "ConfirmedStatusId must be a positive integer.", nameof(confirmedStatusId));

        var now = DateTime.UtcNow;

        if (now < ReservationDate)
            throw new InvalidOperationException(
                "confirmed_at cannot be earlier than reservation_date.");

        ReservationStatusId = confirmedStatusId;
        ConfirmedAt         = now;
        UpdatedAt           = now;
    }

    /// <summary>
    /// Cancela la reserva.
    /// Precondición: no debe estar ya confirmada (exclusión mutua).
    /// Establece cancelled_at = now y actualiza el status al ID proporcionado.
    /// </summary>
    public void Cancel(int cancelledStatusId)
    {
        if (ConfirmedAt.HasValue)
            throw new InvalidOperationException(
                "Cannot cancel a reservation that has already been confirmed.");

        if (CancelledAt.HasValue)
            throw new InvalidOperationException(
                "Reservation is already cancelled.");

        if (cancelledStatusId <= 0)
            throw new ArgumentException(
                "CancelledStatusId must be a positive integer.", nameof(cancelledStatusId));

        var now = DateTime.UtcNow;

        if (now < ReservationDate)
            throw new InvalidOperationException(
                "cancelled_at cannot be earlier than reservation_date.");

        ReservationStatusId = cancelledStatusId;
        CancelledAt         = now;
        UpdatedAt           = now;
    }

    /// <summary>
    /// Cambia el estado de la reserva sin afectar confirmed_at ni cancelled_at.
    /// Usado para transiciones intermedias de estado (ej: PENDING → ON_HOLD).
    /// </summary>
    public void ChangeStatus(int reservationStatusId)
    {
        if (reservationStatusId <= 0)
            throw new ArgumentException(
                "ReservationStatusId must be a positive integer.", nameof(reservationStatusId));

        ReservationStatusId = reservationStatusId;
        UpdatedAt           = DateTime.UtcNow;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("ReservationCode cannot be empty.", nameof(code));

        if (code.Trim().Length > 20)
            throw new ArgumentException("ReservationCode cannot exceed 20 characters.", nameof(code));
    }

    private static void ValidateTimestamps(
        DateTime  reservationDate,
        DateTime? confirmedAt,
        DateTime? cancelledAt)
    {
        // CHECK chk_confirmed_at
        if (confirmedAt.HasValue && confirmedAt.Value < reservationDate)
            throw new ArgumentException(
                "confirmed_at must be >= reservation_date.", nameof(confirmedAt));

        // CHECK chk_cancelled_at
        if (cancelledAt.HasValue && cancelledAt.Value < reservationDate)
            throw new ArgumentException(
                "cancelled_at must be >= reservation_date.", nameof(cancelledAt));

        // CHECK chk_mutual_excl
        if (confirmedAt.HasValue && cancelledAt.HasValue)
            throw new ArgumentException(
                "confirmed_at and cancelled_at are mutually exclusive.");
    }
}
```

---

### RUTA: `src/Modules/Reservation/Domain/Repositories/IReservationRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;

public interface IReservationRepository
{
    Task<ReservationAggregate?>             GetByIdAsync(ReservationId id,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationAggregate>> GetAllAsync(                                        CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationAggregate>> GetByCustomerAsync(int customerId,                  CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationAggregate>> GetByFlightAsync(int scheduledFlightId,             CancellationToken cancellationToken = default);
    Task                                    AddAsync(ReservationAggregate reservation,          CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(ReservationAggregate reservation,       CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(ReservationId id,                       CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Reservation/Application/Interfaces/IReservationService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.Interfaces;

public interface IReservationService
{
    Task<ReservationDto?>             GetByIdAsync(int id,                                             CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDto>> GetAllAsync(                                                     CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDto>> GetByCustomerAsync(int customerId,                               CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDto>> GetByFlightAsync(int scheduledFlightId,                          CancellationToken cancellationToken = default);
    Task<ReservationDto>              CreateAsync(string code, int customerId, int scheduledFlightId, int statusId, CancellationToken cancellationToken = default);
    Task                              ConfirmAsync(int id, int confirmedStatusId,                      CancellationToken cancellationToken = default);
    Task                              CancelAsync(int id, int cancelledStatusId,                       CancellationToken cancellationToken = default);
    Task                              ChangeStatusAsync(int id, int reservationStatusId,               CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                                              CancellationToken cancellationToken = default);
}

public sealed record ReservationDto(
    int      Id,
    string   ReservationCode,
    int      CustomerId,
    int      ScheduledFlightId,
    DateTime ReservationDate,
    int      ReservationStatusId,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    DateTime  CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/CreateReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateReservationUseCase
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateReservationUseCase(IReservationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationAggregate> ExecuteAsync(
        string            reservationCode,
        int               customerId,
        int               scheduledFlightId,
        int               reservationStatusId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // ReservationId(1) es placeholder; EF Core asigna el Id real al insertar.
        var reservation = new ReservationAggregate(
            new ReservationId(1),
            reservationCode,
            customerId,
            scheduledFlightId,
            now,
            reservationStatusId,
            confirmedAt: null,
            cancelledAt: null,
            createdAt:   now);

        await _repository.AddAsync(reservation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return reservation;
    }
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/DeleteReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteReservationUseCase
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteReservationUseCase(IReservationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new ReservationId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/GetAllReservationsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;

public sealed class GetAllReservationsUseCase
{
    private readonly IReservationRepository _repository;

    public GetAllReservationsUseCase(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/GetReservationByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;

public sealed class GetReservationByIdUseCase
{
    private readonly IReservationRepository _repository;

    public GetReservationByIdUseCase(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservationAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new ReservationId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/UpdateReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia el estado de la reserva sin modificar confirmed_at ni cancelled_at.
/// Para confirmar o cancelar usar ConfirmReservationUseCase / CancelReservationUseCase.
/// </summary>
public sealed class UpdateReservationUseCase
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateReservationUseCase(IReservationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               reservationStatusId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _repository.GetByIdAsync(new ReservationId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Reservation with id {id} was not found.");

        reservation.ChangeStatus(reservationStatusId);
        await _repository.UpdateAsync(reservation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/ConfirmReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Confirma una reserva: establece confirmed_at y actualiza el status.
/// El dominio garantiza la exclusión mutua con cancelled_at.
/// </summary>
public sealed class ConfirmReservationUseCase
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public ConfirmReservationUseCase(IReservationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               confirmedStatusId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _repository.GetByIdAsync(new ReservationId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Reservation with id {id} was not found.");

        reservation.Confirm(confirmedStatusId);
        await _repository.UpdateAsync(reservation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/CancelReservationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cancela una reserva: establece cancelled_at y actualiza el status.
/// El dominio garantiza la exclusión mutua con confirmed_at.
/// </summary>
public sealed class CancelReservationUseCase
{
    private readonly IReservationRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CancelReservationUseCase(IReservationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               cancelledStatusId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _repository.GetByIdAsync(new ReservationId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Reservation with id {id} was not found.");

        reservation.Cancel(cancelledStatusId);
        await _repository.UpdateAsync(reservation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/GetReservationsByCustomerUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;

/// <summary>
/// Obtiene todas las reservas de un cliente.
/// Caso de uso clave para el historial del cliente.
/// </summary>
public sealed class GetReservationsByCustomerUseCase
{
    private readonly IReservationRepository _repository;

    public GetReservationsByCustomerUseCase(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationAggregate>> ExecuteAsync(
        int               customerId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCustomerAsync(customerId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Reservation/Application/UseCases/GetReservationsByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;

/// <summary>
/// Obtiene todas las reservas de un vuelo programado.
/// Caso de uso clave para la gestión operativa del vuelo.
/// </summary>
public sealed class GetReservationsByFlightUseCase
{
    private readonly IReservationRepository _repository;

    public GetReservationsByFlightUseCase(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationAggregate>> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Reservation/Application/Services/ReservationService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;

public sealed class ReservationService : IReservationService
{
    private readonly CreateReservationUseCase         _create;
    private readonly DeleteReservationUseCase         _delete;
    private readonly GetAllReservationsUseCase        _getAll;
    private readonly GetReservationByIdUseCase        _getById;
    private readonly UpdateReservationUseCase         _update;
    private readonly ConfirmReservationUseCase        _confirm;
    private readonly CancelReservationUseCase         _cancel;
    private readonly GetReservationsByCustomerUseCase _getByCustomer;
    private readonly GetReservationsByFlightUseCase   _getByFlight;

    public ReservationService(
        CreateReservationUseCase         create,
        DeleteReservationUseCase         delete,
        GetAllReservationsUseCase        getAll,
        GetReservationByIdUseCase        getById,
        UpdateReservationUseCase         update,
        ConfirmReservationUseCase        confirm,
        CancelReservationUseCase         cancel,
        GetReservationsByCustomerUseCase getByCustomer,
        GetReservationsByFlightUseCase   getByFlight)
    {
        _create        = create;
        _delete        = delete;
        _getAll        = getAll;
        _getById       = getById;
        _update        = update;
        _confirm       = confirm;
        _cancel        = cancel;
        _getByCustomer = getByCustomer;
        _getByFlight   = getByFlight;
    }

    public async Task<ReservationDto> CreateAsync(
        string            code,
        int               customerId,
        int               scheduledFlightId,
        int               statusId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(code, customerId, scheduledFlightId, statusId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<ReservationDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<ReservationDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeStatusAsync(
        int               id,
        int               reservationStatusId,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, reservationStatusId, cancellationToken);

    public async Task ConfirmAsync(
        int               id,
        int               confirmedStatusId,
        CancellationToken cancellationToken = default)
        => await _confirm.ExecuteAsync(id, confirmedStatusId, cancellationToken);

    public async Task CancelAsync(
        int               id,
        int               cancelledStatusId,
        CancellationToken cancellationToken = default)
        => await _cancel.ExecuteAsync(id, cancelledStatusId, cancellationToken);

    public async Task<IEnumerable<ReservationDto>> GetByCustomerAsync(
        int               customerId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByCustomer.ExecuteAsync(customerId, cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<ReservationDto>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByFlight.ExecuteAsync(scheduledFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static ReservationDto ToDto(ReservationAggregate agg)
        => new(
            agg.Id.Value,
            agg.ReservationCode,
            agg.CustomerId,
            agg.ScheduledFlightId,
            agg.ReservationDate,
            agg.ReservationStatusId,
            agg.ConfirmedAt,
            agg.CancelledAt,
            agg.CreatedAt,
            agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Reservation/Infrastructure/entity/ReservationEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Infrastructure.Entity;

public sealed class ReservationEntity
{
    public int       Id                  { get; set; }
    public string    ReservationCode     { get; set; } = null!;
    public int       CustomerId          { get; set; }
    public int       ScheduledFlightId   { get; set; }
    public DateTime  ReservationDate     { get; set; }
    public int       ReservationStatusId { get; set; }
    public DateTime? ConfirmedAt         { get; set; }
    public DateTime? CancelledAt         { get; set; }
    public DateTime  CreatedAt           { get; set; }
    public DateTime? UpdatedAt           { get; set; }
}
```

---

### RUTA: `src/Modules/Reservation/Infrastructure/entity/ReservationEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ReservationEntityConfiguration : IEntityTypeConfiguration<ReservationEntity>
{
    public void Configure(EntityTypeBuilder<ReservationEntity> builder)
    {
        builder.ToTable("reservation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("reservation_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ReservationCode)
               .HasColumnName("reservation_code")
               .IsRequired()
               .HasMaxLength(20);

        builder.HasIndex(e => e.ReservationCode)
               .IsUnique()
               .HasDatabaseName("uq_reservation_code");

        builder.Property(e => e.CustomerId)
               .HasColumnName("customer_id")
               .IsRequired();

        builder.Property(e => e.ScheduledFlightId)
               .HasColumnName("scheduled_flight_id")
               .IsRequired();

        builder.Property(e => e.ReservationDate)
               .HasColumnName("reservation_date")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.ReservationStatusId)
               .HasColumnName("reservation_status_id")
               .IsRequired();

        builder.Property(e => e.ConfirmedAt)
               .HasColumnName("confirmed_at")
               .IsRequired(false);

        builder.Property(e => e.CancelledAt)
               .HasColumnName("cancelled_at")
               .IsRequired(false);

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

### RUTA: `src/Modules/Reservation/Infrastructure/repository/ReservationRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _context;

    public ReservationRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static ReservationAggregate ToDomain(ReservationEntity entity)
        => new(
            new ReservationId(entity.Id),
            entity.ReservationCode,
            entity.CustomerId,
            entity.ScheduledFlightId,
            entity.ReservationDate,
            entity.ReservationStatusId,
            entity.ConfirmedAt,
            entity.CancelledAt,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<ReservationAggregate?> GetByIdAsync(
        ReservationId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<ReservationAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Reservations
            .AsNoTracking()
            .OrderByDescending(e => e.ReservationDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<ReservationAggregate>> GetByCustomerAsync(
        int               customerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Reservations
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId)
            .OrderByDescending(e => e.ReservationDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<ReservationAggregate>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Reservations
            .AsNoTracking()
            .Where(e => e.ScheduledFlightId == scheduledFlightId)
            .OrderByDescending(e => e.ReservationDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        ReservationAggregate reservation,
        CancellationToken    cancellationToken = default)
    {
        var entity = new ReservationEntity
        {
            ReservationCode     = reservation.ReservationCode,
            CustomerId          = reservation.CustomerId,
            ScheduledFlightId   = reservation.ScheduledFlightId,
            ReservationDate     = reservation.ReservationDate,
            ReservationStatusId = reservation.ReservationStatusId,
            ConfirmedAt         = reservation.ConfirmedAt,
            CancelledAt         = reservation.CancelledAt,
            CreatedAt           = reservation.CreatedAt,
            UpdatedAt           = reservation.UpdatedAt
        };
        await _context.Reservations.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        ReservationAggregate reservation,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.Reservations
            .FirstOrDefaultAsync(e => e.Id == reservation.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationEntity with id {reservation.Id.Value} not found.");

        // ReservationCode, CustomerId y ScheduledFlightId son inmutables.
        entity.ReservationStatusId = reservation.ReservationStatusId;
        entity.ConfirmedAt         = reservation.ConfirmedAt;
        entity.CancelledAt         = reservation.CancelledAt;
        entity.UpdatedAt           = reservation.UpdatedAt;

        _context.Reservations.Update(entity);
    }

    public async Task DeleteAsync(
        ReservationId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Reservations
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationEntity with id {id.Value} not found.");

        _context.Reservations.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Reservation/UI/ReservationConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Reservation.Application.Interfaces;

public sealed class ReservationConsoleUI
{
    private readonly IReservationService _service;

    public ReservationConsoleUI(IReservationService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== RESERVATION MODULE ==========");
            Console.WriteLine("1.  List all reservations");
            Console.WriteLine("2.  Get reservation by ID");
            Console.WriteLine("3.  List by customer");
            Console.WriteLine("4.  List by flight");
            Console.WriteLine("5.  Create reservation");
            Console.WriteLine("6.  Confirm reservation");
            Console.WriteLine("7.  Cancel reservation");
            Console.WriteLine("8.  Change status");
            Console.WriteLine("9.  Delete reservation");
            Console.WriteLine("0.  Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByCustomerAsync();   break;
                case "4": await ListByFlightAsync();     break;
                case "5": await CreateAsync();           break;
                case "6": await ConfirmAsync();          break;
                case "7": await CancelAsync();           break;
                case "8": await ChangeStatusAsync();     break;
                case "9": await DeleteAsync();           break;
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
        var list = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Reservations ---");
        foreach (var r in list) PrintReservation(r);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter reservation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var r = await _service.GetByIdAsync(id);
        if (r is null) Console.WriteLine($"Reservation with ID {id} not found.");
        else           PrintReservation(r);
    }

    private async Task ListByCustomerAsync()
    {
        Console.Write("Enter Customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int customerId))
        { Console.WriteLine("Invalid ID."); return; }

        var list = await _service.GetByCustomerAsync(customerId);
        Console.WriteLine($"\n--- Reservations for Customer {customerId} ---");
        foreach (var r in list) PrintReservation(r);
    }

    private async Task ListByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        var list = await _service.GetByFlightAsync(flightId);
        Console.WriteLine($"\n--- Reservations for Flight {flightId} ---");
        foreach (var r in list) PrintReservation(r);
    }

    private async Task CreateAsync()
    {
        Console.Write("Reservation code (max 20 chars): ");
        var code = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        { Console.WriteLine("Code cannot be empty."); return; }

        Console.Write("Customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int customerId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Initial Reservation Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        try
        {
            var created = await _service.CreateAsync(code, customerId, flightId, statusId);
            Console.WriteLine($"Reservation created: [{created.Id}] {created.ReservationCode}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task ConfirmAsync()
    {
        Console.Write("Reservation ID to confirm: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Confirmed Status ID (CONFIRMED): ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        try
        {
            await _service.ConfirmAsync(id, statusId);
            Console.WriteLine("Reservation confirmed successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Business rule error: {ex.Message}");
        }
    }

    private async Task CancelAsync()
    {
        Console.Write("Reservation ID to cancel: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Cancelled Status ID (CANCELLED): ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        try
        {
            await _service.CancelAsync(id, statusId);
            Console.WriteLine("Reservation cancelled successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Business rule error: {ex.Message}");
        }
    }

    private async Task ChangeStatusAsync()
    {
        Console.Write("Reservation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.ChangeStatusAsync(id, statusId);
        Console.WriteLine("Status changed successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter reservation ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Reservation deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintReservation(ReservationDto r)
    {
        var state = r.ConfirmedAt.HasValue ? $"CONFIRMED at {r.ConfirmedAt:yyyy-MM-dd HH:mm}"
                  : r.CancelledAt.HasValue ? $"CANCELLED at {r.CancelledAt:yyyy-MM-dd HH:mm}"
                  : "PENDING";

        Console.WriteLine(
            $"  [{r.Id}] {r.ReservationCode} | " +
            $"Customer: {r.CustomerId} | Flight: {r.ScheduledFlightId} | " +
            $"Status: {r.ReservationStatusId} | {state} | " +
            $"Created: {r.CreatedAt:yyyy-MM-dd HH:mm}");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Reservation Module ────────────────────────────────────────────────────────
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<CreateReservationUseCase>();
builder.Services.AddScoped<DeleteReservationUseCase>();
builder.Services.AddScoped<GetAllReservationsUseCase>();
builder.Services.AddScoped<GetReservationByIdUseCase>();
builder.Services.AddScoped<UpdateReservationUseCase>();
builder.Services.AddScoped<ConfirmReservationUseCase>();
builder.Services.AddScoped<CancelReservationUseCase>();
builder.Services.AddScoped<GetReservationsByCustomerUseCase>();
builder.Services.AddScoped<GetReservationsByFlightUseCase>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ReservationConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Reservation/
├── Application/
│   ├── Interfaces/
│   │   └── IReservationService.cs
│   ├── Services/
│   │   └── ReservationService.cs
│   └── UseCases/
│       ├── CancelReservationUseCase.cs
│       ├── ConfirmReservationUseCase.cs
│       ├── CreateReservationUseCase.cs
│       ├── DeleteReservationUseCase.cs
│       ├── GetAllReservationsUseCase.cs
│       ├── GetReservationByIdUseCase.cs
│       ├── GetReservationsByCustomerUseCase.cs
│       ├── GetReservationsByFlightUseCase.cs
│       └── UpdateReservationUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── ReservationAggregate.cs
│   ├── Repositories/
│   │   └── IReservationRepository.cs
│   └── valueObject/
│       └── ReservationId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── ReservationEntity.cs
│   │   └── ReservationEntityConfiguration.cs
│   └── repository/
│       └── ReservationRepository.cs
└── UI/
    └── ReservationConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Reservation_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
