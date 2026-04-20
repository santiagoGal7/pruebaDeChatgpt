# Módulo: FlightSeat
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat`  
**Raíz de archivos:** `src/Modules/FlightSeat/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `flight_seat_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `scheduled_flight_id` | `INT NOT NULL FK` | `int` | FK → `scheduled_flight` |
| `seat_map_id` | `INT NOT NULL FK` | `int` | FK → `seat_map` [IR-1] |
| `seat_status_id` | `INT NOT NULL FK` | `int` | FK → `seat_status` |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Se actualiza al cambiar estado |

**UNIQUE:** `(scheduled_flight_id, seat_map_id)` — cada asiento del mapa aparece como máximo una vez por vuelo.  
**[IR-1]:** `seat_map_id` FK garantiza que el asiento exista en el mapa estático del tipo de aeronave.  
`cabin_class_id` fue eliminado del DDL — se obtiene vía JOIN con `seat_map`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/FlightSeat/Domain/valueObject/FlightSeatId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;

public sealed class FlightSeatId
{
    public int Value { get; }

    public FlightSeatId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("FlightSeatId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is FlightSeatId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/FlightSeat/Domain/aggregate/FlightSeatAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;

/// <summary>
/// Estado dinámico de un asiento concreto en un vuelo programado.
/// SQL: flight_seat.
///
/// [IR-1] seat_map_id FK garantiza que el asiento exista en el mapa
/// estático del tipo de aeronave asignado al vuelo.
/// cabin_class_id fue eliminado del DDL — se obtiene vía seat_map.
///
/// UNIQUE: (scheduled_flight_id, seat_map_id).
/// La única operación de negocio es cambiar el estado del asiento
/// (AVAILABLE → OCCUPIED → BLOCKED) a través de ChangeStatus().
/// scheduled_flight_id y seat_map_id forman la clave de negocio — inmutables.
/// </summary>
public sealed class FlightSeatAggregate
{
    public FlightSeatId Id                { get; private set; }
    public int          ScheduledFlightId { get; private set; }
    public int          SeatMapId         { get; private set; }
    public int          SeatStatusId      { get; private set; }
    public DateTime     CreatedAt         { get; private set; }
    public DateTime?    UpdatedAt         { get; private set; }

    private FlightSeatAggregate()
    {
        Id = null!;
    }

    public FlightSeatAggregate(
        FlightSeatId id,
        int          scheduledFlightId,
        int          seatMapId,
        int          seatStatusId,
        DateTime     createdAt,
        DateTime?    updatedAt = null)
    {
        if (scheduledFlightId <= 0)
            throw new ArgumentException(
                "ScheduledFlightId must be a positive integer.", nameof(scheduledFlightId));

        if (seatMapId <= 0)
            throw new ArgumentException(
                "SeatMapId must be a positive integer.", nameof(seatMapId));

        if (seatStatusId <= 0)
            throw new ArgumentException(
                "SeatStatusId must be a positive integer.", nameof(seatStatusId));

        Id                = id;
        ScheduledFlightId = scheduledFlightId;
        SeatMapId         = seatMapId;
        SeatStatusId      = seatStatusId;
        CreatedAt         = createdAt;
        UpdatedAt         = updatedAt;
    }

    /// <summary>
    /// Cambia el estado del asiento en este vuelo.
    /// Es la única operación de negocio válida sobre flight_seat.
    /// Ejemplo: AVAILABLE → OCCUPIED (al reservar), OCCUPIED → AVAILABLE (al cancelar).
    /// </summary>
    public void ChangeStatus(int seatStatusId)
    {
        if (seatStatusId <= 0)
            throw new ArgumentException(
                "SeatStatusId must be a positive integer.", nameof(seatStatusId));

        SeatStatusId = seatStatusId;
        UpdatedAt    = DateTime.UtcNow;
    }
}
```

---

### RUTA: `src/Modules/FlightSeat/Domain/Repositories/IFlightSeatRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;

public interface IFlightSeatRepository
{
    Task<FlightSeatAggregate?>             GetByIdAsync(FlightSeatId id,                        CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatAggregate>> GetAllAsync(                                          CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatAggregate>> GetByFlightAsync(int scheduledFlightId,               CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatAggregate>> GetAvailableByFlightAsync(int scheduledFlightId,      CancellationToken cancellationToken = default);
    Task                                   AddAsync(FlightSeatAggregate flightSeat,              CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(FlightSeatAggregate flightSeat,           CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(FlightSeatId id,                          CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/FlightSeat/Application/Interfaces/IFlightSeatService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.Interfaces;

public interface IFlightSeatService
{
    Task<FlightSeatDto?>             GetByIdAsync(int id,                                          CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatDto>> GetAllAsync(                                                  CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatDto>> GetByFlightAsync(int scheduledFlightId,                       CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightSeatDto>> GetAvailableByFlightAsync(int scheduledFlightId,              CancellationToken cancellationToken = default);
    Task<FlightSeatDto>              CreateAsync(int scheduledFlightId, int seatMapId, int seatStatusId, CancellationToken cancellationToken = default);
    Task                             ChangeStatusAsync(int id, int seatStatusId,                   CancellationToken cancellationToken = default);
    Task                             DeleteAsync(int id,                                           CancellationToken cancellationToken = default);
}

public sealed record FlightSeatDto(
    int      Id,
    int      ScheduledFlightId,
    int      SeatMapId,
    int      SeatStatusId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/CreateFlightSeatUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateFlightSeatUseCase
{
    private readonly IFlightSeatRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public CreateFlightSeatUseCase(IFlightSeatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlightSeatAggregate> ExecuteAsync(
        int               scheduledFlightId,
        int               seatMapId,
        int               seatStatusId,
        CancellationToken cancellationToken = default)
    {
        // FlightSeatId(1) es placeholder; EF Core asigna el Id real al insertar.
        var flightSeat = new FlightSeatAggregate(
            new FlightSeatId(1),
            scheduledFlightId,
            seatMapId,
            seatStatusId,
            DateTime.UtcNow);

        await _repository.AddAsync(flightSeat, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return flightSeat;
    }
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/DeleteFlightSeatUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteFlightSeatUseCase
{
    private readonly IFlightSeatRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public DeleteFlightSeatUseCase(IFlightSeatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new FlightSeatId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/GetAllFlightSeatsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;

public sealed class GetAllFlightSeatsUseCase
{
    private readonly IFlightSeatRepository _repository;

    public GetAllFlightSeatsUseCase(IFlightSeatRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightSeatAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/GetFlightSeatByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;

public sealed class GetFlightSeatByIdUseCase
{
    private readonly IFlightSeatRepository _repository;

    public GetFlightSeatByIdUseCase(IFlightSeatRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlightSeatAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new FlightSeatId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/ChangeFlightSeatStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia el estado de un asiento en un vuelo concreto.
/// Es la única mutación válida sobre flight_seat tras su creación.
/// Ejemplos: AVAILABLE → OCCUPIED (reserva), OCCUPIED → AVAILABLE (cancelación).
/// El trigger RF-6 en la BD verifica que el asiento esté AVAILABLE antes
/// de insertar en reservation_detail.
/// </summary>
public sealed class ChangeFlightSeatStatusUseCase
{
    private readonly IFlightSeatRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public ChangeFlightSeatStatusUseCase(IFlightSeatRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               seatStatusId,
        CancellationToken cancellationToken = default)
    {
        var flightSeat = await _repository.GetByIdAsync(new FlightSeatId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"FlightSeat with id {id} was not found.");

        flightSeat.ChangeStatus(seatStatusId);
        await _repository.UpdateAsync(flightSeat, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/GetFlightSeatsByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;

/// <summary>
/// Obtiene todos los asientos (con su estado actual) de un vuelo programado.
/// </summary>
public sealed class GetFlightSeatsByFlightUseCase
{
    private readonly IFlightSeatRepository _repository;

    public GetFlightSeatsByFlightUseCase(IFlightSeatRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightSeatAggregate>> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/UseCases/GetAvailableFlightSeatsByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;

/// <summary>
/// Obtiene únicamente los asientos disponibles de un vuelo programado.
/// Caso de uso clave para el proceso de reserva: permite al cliente
/// elegir entre los asientos libres antes de confirmar.
/// </summary>
public sealed class GetAvailableFlightSeatsByFlightUseCase
{
    private readonly IFlightSeatRepository _repository;

    public GetAvailableFlightSeatsByFlightUseCase(IFlightSeatRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightSeatAggregate>> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetAvailableByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightSeat/Application/Services/FlightSeatService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;

public sealed class FlightSeatService : IFlightSeatService
{
    private readonly CreateFlightSeatUseCase                  _create;
    private readonly DeleteFlightSeatUseCase                  _delete;
    private readonly GetAllFlightSeatsUseCase                 _getAll;
    private readonly GetFlightSeatByIdUseCase                 _getById;
    private readonly ChangeFlightSeatStatusUseCase            _changeStatus;
    private readonly GetFlightSeatsByFlightUseCase            _getByFlight;
    private readonly GetAvailableFlightSeatsByFlightUseCase   _getAvailable;

    public FlightSeatService(
        CreateFlightSeatUseCase                create,
        DeleteFlightSeatUseCase                delete,
        GetAllFlightSeatsUseCase               getAll,
        GetFlightSeatByIdUseCase               getById,
        ChangeFlightSeatStatusUseCase          changeStatus,
        GetFlightSeatsByFlightUseCase          getByFlight,
        GetAvailableFlightSeatsByFlightUseCase getAvailable)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _changeStatus = changeStatus;
        _getByFlight  = getByFlight;
        _getAvailable = getAvailable;
    }

    public async Task<FlightSeatDto> CreateAsync(
        int               scheduledFlightId,
        int               seatMapId,
        int               seatStatusId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(scheduledFlightId, seatMapId, seatStatusId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<FlightSeatDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<FlightSeatDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeStatusAsync(
        int               id,
        int               seatStatusId,
        CancellationToken cancellationToken = default)
        => await _changeStatus.ExecuteAsync(id, seatStatusId, cancellationToken);

    public async Task<IEnumerable<FlightSeatDto>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByFlight.ExecuteAsync(scheduledFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<FlightSeatDto>> GetAvailableByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getAvailable.ExecuteAsync(scheduledFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static FlightSeatDto ToDto(FlightSeatAggregate agg)
        => new(agg.Id.Value, agg.ScheduledFlightId, agg.SeatMapId,
               agg.SeatStatusId, agg.CreatedAt, agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/FlightSeat/Infrastructure/entity/FlightSeatEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Infrastructure.Entity;

public sealed class FlightSeatEntity
{
    public int       Id                { get; set; }
    public int       ScheduledFlightId { get; set; }
    public int       SeatMapId         { get; set; }
    public int       SeatStatusId      { get; set; }
    public DateTime  CreatedAt         { get; set; }
    public DateTime? UpdatedAt         { get; set; }
}
```

---

### RUTA: `src/Modules/FlightSeat/Infrastructure/entity/FlightSeatEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class FlightSeatEntityConfiguration : IEntityTypeConfiguration<FlightSeatEntity>
{
    public void Configure(EntityTypeBuilder<FlightSeatEntity> builder)
    {
        builder.ToTable("flight_seat");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("flight_seat_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ScheduledFlightId)
               .HasColumnName("scheduled_flight_id")
               .IsRequired();

        builder.Property(e => e.SeatMapId)
               .HasColumnName("seat_map_id")
               .IsRequired();

        builder.Property(e => e.SeatStatusId)
               .HasColumnName("seat_status_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);

        // UNIQUE (scheduled_flight_id, seat_map_id) — espejo de uq_flight_seat
        builder.HasIndex(e => new { e.ScheduledFlightId, e.SeatMapId })
               .IsUnique()
               .HasDatabaseName("uq_flight_seat");
    }
}
```

---

### RUTA: `src/Modules/FlightSeat/Infrastructure/repository/FlightSeatRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class FlightSeatRepository : IFlightSeatRepository
{
    private readonly AppDbContext _context;

    // seat_status_id correspondiente a AVAILABLE.
    // Se filtra por nombre en GetAvailableByFlightAsync usando una subconsulta
    // para no acoplar el repositorio a un valor numérico hardcodeado.
    private const string AvailableStatusName = "AVAILABLE";

    public FlightSeatRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static FlightSeatAggregate ToDomain(FlightSeatEntity entity)
        => new(
            new FlightSeatId(entity.Id),
            entity.ScheduledFlightId,
            entity.SeatMapId,
            entity.SeatStatusId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<FlightSeatAggregate?> GetByIdAsync(
        FlightSeatId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightSeats
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<FlightSeatAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightSeats
            .AsNoTracking()
            .OrderBy(e => e.ScheduledFlightId)
            .ThenBy(e => e.SeatMapId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<FlightSeatAggregate>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightSeats
            .AsNoTracking()
            .Where(e => e.ScheduledFlightId == scheduledFlightId)
            .OrderBy(e => e.SeatMapId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<FlightSeatAggregate>> GetAvailableByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        // Filtra por el nombre del estado para evitar hardcodear un ID numérico.
        var availableStatusId = await _context.SeatStatuses
            .AsNoTracking()
            .Where(ss => ss.Name == AvailableStatusName)
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var entities = await _context.FlightSeats
            .AsNoTracking()
            .Where(e => e.ScheduledFlightId == scheduledFlightId
                     && e.SeatStatusId       == availableStatusId)
            .OrderBy(e => e.SeatMapId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        FlightSeatAggregate flightSeat,
        CancellationToken   cancellationToken = default)
    {
        var entity = new FlightSeatEntity
        {
            ScheduledFlightId = flightSeat.ScheduledFlightId,
            SeatMapId         = flightSeat.SeatMapId,
            SeatStatusId      = flightSeat.SeatStatusId,
            CreatedAt         = flightSeat.CreatedAt,
            UpdatedAt         = flightSeat.UpdatedAt
        };
        await _context.FlightSeats.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        FlightSeatAggregate flightSeat,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.FlightSeats
            .FirstOrDefaultAsync(e => e.Id == flightSeat.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightSeatEntity with id {flightSeat.Id.Value} not found.");

        // Solo seat_status_id y updated_at son mutables.
        // scheduled_flight_id y seat_map_id forman la clave de negocio.
        entity.SeatStatusId = flightSeat.SeatStatusId;
        entity.UpdatedAt    = flightSeat.UpdatedAt;

        _context.FlightSeats.Update(entity);
    }

    public async Task DeleteAsync(
        FlightSeatId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightSeats
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightSeatEntity with id {id.Value} not found.");

        _context.FlightSeats.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/FlightSeat/UI/FlightSeatConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightSeat.Application.Interfaces;

public sealed class FlightSeatConsoleUI
{
    private readonly IFlightSeatService _service;

    public FlightSeatConsoleUI(IFlightSeatService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== FLIGHT SEAT MODULE ==========");
            Console.WriteLine("1. List all flight seats");
            Console.WriteLine("2. Get flight seat by ID");
            Console.WriteLine("3. List seats by flight");
            Console.WriteLine("4. List available seats by flight");
            Console.WriteLine("5. Create flight seat");
            Console.WriteLine("6. Change seat status");
            Console.WriteLine("7. Delete flight seat");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();               break;
                case "2": await GetByIdAsync();               break;
                case "3": await ListByFlightAsync();          break;
                case "4": await ListAvailableByFlightAsync(); break;
                case "5": await CreateAsync();                break;
                case "6": await ChangeStatusAsync();          break;
                case "7": await DeleteAsync();                break;
                case "0": running = false;                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var seats = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Flight Seats ---");

        foreach (var s in seats)
            PrintSeat(s);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter flight seat ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var seat = await _service.GetByIdAsync(id);

        if (seat is null)
            Console.WriteLine($"Flight seat with ID {id} not found.");
        else
            PrintSeat(seat);
    }

    private async Task ListByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var seats = await _service.GetByFlightAsync(flightId);
        Console.WriteLine($"\n--- All Seats for Flight {flightId} ---");

        foreach (var s in seats)
            PrintSeat(s);
    }

    private async Task ListAvailableByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var seats = await _service.GetAvailableByFlightAsync(flightId);
        var list  = seats.ToList();
        Console.WriteLine($"\n--- Available Seats for Flight {flightId} ({list.Count} available) ---");

        foreach (var s in list)
            PrintSeat(s);
    }

    private async Task CreateAsync()
    {
        Console.Write("Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Seat Map ID: ");
        if (!int.TryParse(Console.ReadLine(), out int seatMapId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Seat Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        var created = await _service.CreateAsync(flightId, seatMapId, statusId);
        Console.WriteLine(
            $"Flight seat created: [{created.Id}] " +
            $"Flight: {created.ScheduledFlightId} | SeatMap: {created.SeatMapId} | Status: {created.SeatStatusId}");
    }

    private async Task ChangeStatusAsync()
    {
        Console.Write("Flight seat ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Seat Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.ChangeStatusAsync(id, statusId);
        Console.WriteLine("Flight seat status changed successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter flight seat ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Flight seat deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintSeat(FlightSeatDto s)
        => Console.WriteLine(
            $"  [{s.Id}] Flight: {s.ScheduledFlightId} | SeatMap: {s.SeatMapId} | " +
            $"Status: {s.SeatStatusId} | Created: {s.CreatedAt:yyyy-MM-dd HH:mm}" +
            (s.UpdatedAt.HasValue ? $" | Updated: {s.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── FlightSeat Module ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IFlightSeatRepository, FlightSeatRepository>();
builder.Services.AddScoped<CreateFlightSeatUseCase>();
builder.Services.AddScoped<DeleteFlightSeatUseCase>();
builder.Services.AddScoped<GetAllFlightSeatsUseCase>();
builder.Services.AddScoped<GetFlightSeatByIdUseCase>();
builder.Services.AddScoped<ChangeFlightSeatStatusUseCase>();
builder.Services.AddScoped<GetFlightSeatsByFlightUseCase>();
builder.Services.AddScoped<GetAvailableFlightSeatsByFlightUseCase>();
builder.Services.AddScoped<IFlightSeatService, FlightSeatService>();
builder.Services.AddScoped<FlightSeatConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/FlightSeat/
├── Application/
│   ├── Interfaces/
│   │   └── IFlightSeatService.cs
│   ├── Services/
│   │   └── FlightSeatService.cs
│   └── UseCases/
│       ├── ChangeFlightSeatStatusUseCase.cs
│       ├── CreateFlightSeatUseCase.cs
│       ├── DeleteFlightSeatUseCase.cs
│       ├── GetAllFlightSeatsUseCase.cs
│       ├── GetAvailableFlightSeatsByFlightUseCase.cs
│       ├── GetFlightSeatByIdUseCase.cs
│       └── GetFlightSeatsByFlightUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── FlightSeatAggregate.cs
│   ├── Repositories/
│   │   └── IFlightSeatRepository.cs
│   └── valueObject/
│       └── FlightSeatId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── FlightSeatEntity.cs
│   │   └── FlightSeatEntityConfiguration.cs
│   └── repository/
│       └── FlightSeatRepository.cs
└── UI/
    └── FlightSeatConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo FlightSeat_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
