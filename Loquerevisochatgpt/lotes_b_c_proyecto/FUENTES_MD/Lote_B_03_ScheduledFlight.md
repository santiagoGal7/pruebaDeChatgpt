# Módulo: ScheduledFlight
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight`  
**Raíz de archivos:** `src/Modules/ScheduledFlight/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `scheduled_flight_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `base_flight_id` | `INT NOT NULL FK` | `int` | FK → `base_flight` |
| `aircraft_id` | `INT NOT NULL FK` | `int` | FK → `aircraft` |
| `gate_id` | `INT NULL FK` | `int?` | FK → `gate`, nullable (puede no estar asignada al crear) |
| `departure_date` | `DATE NOT NULL` | `DateOnly` | .NET 8 — Pomelo mapea DATE ↔ DateOnly de forma nativa |
| `departure_time` | `TIME NOT NULL` | `TimeOnly` | .NET 8 — Pomelo mapea TIME ↔ TimeOnly de forma nativa |
| `estimated_arrival_datetime` | `DATETIME NOT NULL` | `DateTime` | Soporta vuelos nocturnos e internacionales |
| `flight_status_id` | `INT NOT NULL FK` | `int` | FK → `flight_status` |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**UNIQUE:** `(base_flight_id, departure_date, departure_time)`

---

## 1. DOMAIN

---

### RUTA: `src/Modules/ScheduledFlight/Domain/valueObject/ScheduledFlightId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;

public sealed class ScheduledFlightId
{
    public int Value { get; }

    public ScheduledFlightId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("ScheduledFlightId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is ScheduledFlightId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Domain/aggregate/ScheduledFlightAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;

/// <summary>
/// Instancia concreta de un vuelo programado para una fecha específica.
/// SQL: scheduled_flight.
///
/// Tipos .NET 8:
///   - departure_date  → DateOnly  (DATE SQL)
///   - departure_time  → TimeOnly  (TIME SQL)
///   - estimated_arrival_datetime → DateTime (DATETIME SQL — soporta vuelos nocturnos)
///
/// Invariantes:
///   - estimated_arrival_datetime debe ser posterior al momento de salida combinado.
///   - gate_id es nullable (puede asignarse después de crear el vuelo).
///   - UNIQUE (base_flight_id, departure_date, departure_time).
/// </summary>
public sealed class ScheduledFlightAggregate
{
    public ScheduledFlightId Id                       { get; private set; }
    public int               BaseFlightId             { get; private set; }
    public int               AircraftId               { get; private set; }
    public int?              GateId                   { get; private set; }
    public DateOnly          DepartureDate            { get; private set; }
    public TimeOnly          DepartureTime            { get; private set; }
    public DateTime          EstimatedArrivalDatetime { get; private set; }
    public int               FlightStatusId           { get; private set; }
    public DateTime          CreatedAt                { get; private set; }
    public DateTime?         UpdatedAt                { get; private set; }

    private ScheduledFlightAggregate()
    {
        Id = null!;
    }

    public ScheduledFlightAggregate(
        ScheduledFlightId id,
        int               baseFlightId,
        int               aircraftId,
        int?              gateId,
        DateOnly          departureDate,
        TimeOnly          departureTime,
        DateTime          estimatedArrivalDatetime,
        int               flightStatusId,
        DateTime          createdAt,
        DateTime?         updatedAt = null)
    {
        if (baseFlightId <= 0)
            throw new ArgumentException("BaseFlightId must be a positive integer.", nameof(baseFlightId));

        if (aircraftId <= 0)
            throw new ArgumentException("AircraftId must be a positive integer.", nameof(aircraftId));

        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException("GateId must be a positive integer when provided.", nameof(gateId));

        if (flightStatusId <= 0)
            throw new ArgumentException("FlightStatusId must be a positive integer.", nameof(flightStatusId));

        ValidateArrivalAfterDeparture(departureDate, departureTime, estimatedArrivalDatetime);

        Id                       = id;
        BaseFlightId             = baseFlightId;
        AircraftId               = aircraftId;
        GateId                   = gateId;
        DepartureDate            = departureDate;
        DepartureTime            = departureTime;
        EstimatedArrivalDatetime = estimatedArrivalDatetime;
        FlightStatusId           = flightStatusId;
        CreatedAt                = createdAt;
        UpdatedAt                = updatedAt;
    }

    /// <summary>
    /// Actualiza los datos operativos del vuelo programado.
    /// base_flight_id no se permite cambiar (cambiaría la identidad del vuelo).
    /// </summary>
    public void Update(
        int      aircraftId,
        int?     gateId,
        DateOnly departureDate,
        TimeOnly departureTime,
        DateTime estimatedArrivalDatetime,
        int      flightStatusId)
    {
        if (aircraftId <= 0)
            throw new ArgumentException("AircraftId must be a positive integer.", nameof(aircraftId));

        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException("GateId must be a positive integer when provided.", nameof(gateId));

        if (flightStatusId <= 0)
            throw new ArgumentException("FlightStatusId must be a positive integer.", nameof(flightStatusId));

        ValidateArrivalAfterDeparture(departureDate, departureTime, estimatedArrivalDatetime);

        AircraftId               = aircraftId;
        GateId                   = gateId;
        DepartureDate            = departureDate;
        DepartureTime            = departureTime;
        EstimatedArrivalDatetime = estimatedArrivalDatetime;
        FlightStatusId           = flightStatusId;
        UpdatedAt                = DateTime.UtcNow;
    }

    /// <summary>Asigna o cambia la puerta de embarque sin tocar otros campos.</summary>
    public void AssignGate(int? gateId)
    {
        if (gateId.HasValue && gateId.Value <= 0)
            throw new ArgumentException("GateId must be a positive integer when provided.", nameof(gateId));

        GateId    = gateId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Cambia el estado del vuelo (SCHEDULED → ACTIVE → COMPLETED, etc.).</summary>
    public void ChangeStatus(int flightStatusId)
    {
        if (flightStatusId <= 0)
            throw new ArgumentException("FlightStatusId must be a positive integer.", nameof(flightStatusId));

        FlightStatusId = flightStatusId;
        UpdatedAt      = DateTime.UtcNow;
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static void ValidateArrivalAfterDeparture(
        DateOnly departureDate,
        TimeOnly departureTime,
        DateTime estimatedArrival)
    {
        // Combina DateOnly + TimeOnly para construir el DateTime de salida (UTC).
        var departureDateTime = departureDate.ToDateTime(departureTime, DateTimeKind.Utc);

        if (estimatedArrival <= departureDateTime)
            throw new ArgumentException(
                "EstimatedArrivalDatetime must be after the departure datetime.",
                nameof(estimatedArrival));
    }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Domain/Repositories/IScheduledFlightRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;

public interface IScheduledFlightRepository
{
    Task<ScheduledFlightAggregate?>             GetByIdAsync(ScheduledFlightId id,                       CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightAggregate>> GetAllAsync(                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightAggregate>> GetByBaseFlightAsync(int baseFlightId,                    CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightAggregate>> GetByDateAsync(DateOnly date,                             CancellationToken cancellationToken = default);
    Task                                        AddAsync(ScheduledFlightAggregate scheduledFlight,        CancellationToken cancellationToken = default);
    Task                                        UpdateAsync(ScheduledFlightAggregate scheduledFlight,     CancellationToken cancellationToken = default);
    Task                                        DeleteAsync(ScheduledFlightId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/ScheduledFlight/Application/Interfaces/IScheduledFlightService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Interfaces;

public interface IScheduledFlightService
{
    Task<ScheduledFlightDto?>             GetByIdAsync(int id,                                       CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightDto>> GetAllAsync(                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightDto>> GetByBaseFlightAsync(int baseFlightId,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<ScheduledFlightDto>> GetByDateAsync(DateOnly date,                              CancellationToken cancellationToken = default);
    Task<ScheduledFlightDto>              CreateAsync(CreateScheduledFlightRequest request,          CancellationToken cancellationToken = default);
    Task                                  UpdateAsync(int id, UpdateScheduledFlightRequest request,  CancellationToken cancellationToken = default);
    Task                                  DeleteAsync(int id,                                        CancellationToken cancellationToken = default);
}

public sealed record ScheduledFlightDto(
    int      Id,
    int      BaseFlightId,
    int      AircraftId,
    int?     GateId,
    DateOnly DepartureDate,
    TimeOnly DepartureTime,
    DateTime EstimatedArrivalDatetime,
    int      FlightStatusId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateScheduledFlightRequest(
    int      BaseFlightId,
    int      AircraftId,
    int?     GateId,
    DateOnly DepartureDate,
    TimeOnly DepartureTime,
    DateTime EstimatedArrivalDatetime,
    int      FlightStatusId);

public sealed record UpdateScheduledFlightRequest(
    int      AircraftId,
    int?     GateId,
    DateOnly DepartureDate,
    TimeOnly DepartureTime,
    DateTime EstimatedArrivalDatetime,
    int      FlightStatusId);
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/CreateScheduledFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateScheduledFlightUseCase
{
    private readonly IScheduledFlightRepository _repository;
    private readonly IUnitOfWork                _unitOfWork;

    public CreateScheduledFlightUseCase(IScheduledFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduledFlightAggregate> ExecuteAsync(
        CreateScheduledFlightRequest request,
        CancellationToken            cancellationToken = default)
    {
        // ScheduledFlightId(1) es placeholder; EF Core asigna el Id real al insertar.
        var scheduledFlight = new ScheduledFlightAggregate(
            new ScheduledFlightId(1),
            request.BaseFlightId,
            request.AircraftId,
            request.GateId,
            request.DepartureDate,
            request.DepartureTime,
            request.EstimatedArrivalDatetime,
            request.FlightStatusId,
            DateTime.UtcNow);

        await _repository.AddAsync(scheduledFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return scheduledFlight;
    }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/DeleteScheduledFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteScheduledFlightUseCase
{
    private readonly IScheduledFlightRepository _repository;
    private readonly IUnitOfWork                _unitOfWork;

    public DeleteScheduledFlightUseCase(IScheduledFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new ScheduledFlightId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/GetAllScheduledFlightsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;

public sealed class GetAllScheduledFlightsUseCase
{
    private readonly IScheduledFlightRepository _repository;

    public GetAllScheduledFlightsUseCase(IScheduledFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/GetScheduledFlightByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;

public sealed class GetScheduledFlightByIdUseCase
{
    private readonly IScheduledFlightRepository _repository;

    public GetScheduledFlightByIdUseCase(IScheduledFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<ScheduledFlightAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new ScheduledFlightId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/UpdateScheduledFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateScheduledFlightUseCase
{
    private readonly IScheduledFlightRepository _repository;
    private readonly IUnitOfWork                _unitOfWork;

    public UpdateScheduledFlightUseCase(IScheduledFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int                          id,
        UpdateScheduledFlightRequest request,
        CancellationToken            cancellationToken = default)
    {
        var scheduledFlight = await _repository.GetByIdAsync(new ScheduledFlightId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"ScheduledFlight with id {id} was not found.");

        scheduledFlight.Update(
            request.AircraftId,
            request.GateId,
            request.DepartureDate,
            request.DepartureTime,
            request.EstimatedArrivalDatetime,
            request.FlightStatusId);

        await _repository.UpdateAsync(scheduledFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/GetScheduledFlightsByBaseFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;

/// <summary>
/// Obtiene todas las instancias de vuelo asociadas a un vuelo base.
/// Útil para ver el historial de operaciones de un vuelo recurrente.
/// </summary>
public sealed class GetScheduledFlightsByBaseFlightUseCase
{
    private readonly IScheduledFlightRepository _repository;

    public GetScheduledFlightsByBaseFlightUseCase(IScheduledFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> ExecuteAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByBaseFlightAsync(baseFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/UseCases/GetScheduledFlightsByDateUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;

/// <summary>
/// Obtiene todos los vuelos programados para una fecha concreta.
/// Punto de entrada clave para la operación diaria del aeropuerto.
/// </summary>
public sealed class GetScheduledFlightsByDateUseCase
{
    private readonly IScheduledFlightRepository _repository;

    public GetScheduledFlightsByDateUseCase(IScheduledFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> ExecuteAsync(
        DateOnly          date,
        CancellationToken cancellationToken = default)
        => await _repository.GetByDateAsync(date, cancellationToken);
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Application/Services/ScheduledFlightService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;

public sealed class ScheduledFlightService : IScheduledFlightService
{
    private readonly CreateScheduledFlightUseCase             _create;
    private readonly DeleteScheduledFlightUseCase             _delete;
    private readonly GetAllScheduledFlightsUseCase            _getAll;
    private readonly GetScheduledFlightByIdUseCase            _getById;
    private readonly UpdateScheduledFlightUseCase             _update;
    private readonly GetScheduledFlightsByBaseFlightUseCase   _getByBaseFlight;
    private readonly GetScheduledFlightsByDateUseCase         _getByDate;

    public ScheduledFlightService(
        CreateScheduledFlightUseCase           create,
        DeleteScheduledFlightUseCase           delete,
        GetAllScheduledFlightsUseCase          getAll,
        GetScheduledFlightByIdUseCase          getById,
        UpdateScheduledFlightUseCase           update,
        GetScheduledFlightsByBaseFlightUseCase getByBaseFlight,
        GetScheduledFlightsByDateUseCase       getByDate)
    {
        _create          = create;
        _delete          = delete;
        _getAll          = getAll;
        _getById         = getById;
        _update          = update;
        _getByBaseFlight = getByBaseFlight;
        _getByDate       = getByDate;
    }

    public async Task<ScheduledFlightDto> CreateAsync(
        CreateScheduledFlightRequest request,
        CancellationToken            cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(request, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<ScheduledFlightDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<ScheduledFlightDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int                          id,
        UpdateScheduledFlightRequest request,
        CancellationToken            cancellationToken = default)
        => await _update.ExecuteAsync(id, request, cancellationToken);

    public async Task<IEnumerable<ScheduledFlightDto>> GetByBaseFlightAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByBaseFlight.ExecuteAsync(baseFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<IEnumerable<ScheduledFlightDto>> GetByDateAsync(
        DateOnly          date,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByDate.ExecuteAsync(date, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static ScheduledFlightDto ToDto(ScheduledFlightAggregate agg)
        => new(
            agg.Id.Value,
            agg.BaseFlightId,
            agg.AircraftId,
            agg.GateId,
            agg.DepartureDate,
            agg.DepartureTime,
            agg.EstimatedArrivalDatetime,
            agg.FlightStatusId,
            agg.CreatedAt,
            agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/ScheduledFlight/Infrastructure/entity/ScheduledFlightEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Infrastructure.Entity;

public sealed class ScheduledFlightEntity
{
    public int       Id                       { get; set; }
    public int       BaseFlightId             { get; set; }
    public int       AircraftId               { get; set; }
    public int?      GateId                   { get; set; }
    public DateOnly  DepartureDate            { get; set; }
    public TimeOnly  DepartureTime            { get; set; }
    public DateTime  EstimatedArrivalDatetime { get; set; }
    public int       FlightStatusId           { get; set; }
    public DateTime  CreatedAt                { get; set; }
    public DateTime? UpdatedAt                { get; set; }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Infrastructure/entity/ScheduledFlightEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ScheduledFlightEntityConfiguration : IEntityTypeConfiguration<ScheduledFlightEntity>
{
    public void Configure(EntityTypeBuilder<ScheduledFlightEntity> builder)
    {
        builder.ToTable("scheduled_flight");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("scheduled_flight_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.BaseFlightId)
               .HasColumnName("base_flight_id")
               .IsRequired();

        builder.Property(e => e.AircraftId)
               .HasColumnName("aircraft_id")
               .IsRequired();

        builder.Property(e => e.GateId)
               .HasColumnName("gate_id")
               .IsRequired(false);

        // Pomelo 8.x mapea DateOnly ↔ DATE de MySQL de forma nativa.
        builder.Property(e => e.DepartureDate)
               .HasColumnName("departure_date")
               .IsRequired();

        // Pomelo 8.x mapea TimeOnly ↔ TIME de MySQL de forma nativa.
        builder.Property(e => e.DepartureTime)
               .HasColumnName("departure_time")
               .IsRequired();

        builder.Property(e => e.EstimatedArrivalDatetime)
               .HasColumnName("estimated_arrival_datetime")
               .IsRequired();

        builder.Property(e => e.FlightStatusId)
               .HasColumnName("flight_status_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);

        // UNIQUE (base_flight_id, departure_date, departure_time) — espejo de uq_sf
        builder.HasIndex(e => new { e.BaseFlightId, e.DepartureDate, e.DepartureTime })
               .IsUnique()
               .HasDatabaseName("uq_sf");
    }
}
```

---

### RUTA: `src/Modules/ScheduledFlight/Infrastructure/repository/ScheduledFlightRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class ScheduledFlightRepository : IScheduledFlightRepository
{
    private readonly AppDbContext _context;

    public ScheduledFlightRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static ScheduledFlightAggregate ToDomain(ScheduledFlightEntity entity)
        => new(
            new ScheduledFlightId(entity.Id),
            entity.BaseFlightId,
            entity.AircraftId,
            entity.GateId,
            entity.DepartureDate,
            entity.DepartureTime,
            entity.EstimatedArrivalDatetime,
            entity.FlightStatusId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<ScheduledFlightAggregate?> GetByIdAsync(
        ScheduledFlightId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ScheduledFlights
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ScheduledFlights
            .AsNoTracking()
            .OrderByDescending(e => e.DepartureDate)
            .ThenBy(e => e.DepartureTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> GetByBaseFlightAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ScheduledFlights
            .AsNoTracking()
            .Where(e => e.BaseFlightId == baseFlightId)
            .OrderByDescending(e => e.DepartureDate)
            .ThenBy(e => e.DepartureTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<ScheduledFlightAggregate>> GetByDateAsync(
        DateOnly          date,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ScheduledFlights
            .AsNoTracking()
            .Where(e => e.DepartureDate == date)
            .OrderBy(e => e.DepartureTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        ScheduledFlightAggregate scheduledFlight,
        CancellationToken        cancellationToken = default)
    {
        var entity = new ScheduledFlightEntity
        {
            BaseFlightId             = scheduledFlight.BaseFlightId,
            AircraftId               = scheduledFlight.AircraftId,
            GateId                   = scheduledFlight.GateId,
            DepartureDate            = scheduledFlight.DepartureDate,
            DepartureTime            = scheduledFlight.DepartureTime,
            EstimatedArrivalDatetime = scheduledFlight.EstimatedArrivalDatetime,
            FlightStatusId           = scheduledFlight.FlightStatusId,
            CreatedAt                = scheduledFlight.CreatedAt,
            UpdatedAt                = scheduledFlight.UpdatedAt
        };
        await _context.ScheduledFlights.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        ScheduledFlightAggregate scheduledFlight,
        CancellationToken        cancellationToken = default)
    {
        var entity = await _context.ScheduledFlights
            .FirstOrDefaultAsync(e => e.Id == scheduledFlight.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ScheduledFlightEntity with id {scheduledFlight.Id.Value} not found.");

        entity.AircraftId               = scheduledFlight.AircraftId;
        entity.GateId                   = scheduledFlight.GateId;
        entity.DepartureDate            = scheduledFlight.DepartureDate;
        entity.DepartureTime            = scheduledFlight.DepartureTime;
        entity.EstimatedArrivalDatetime = scheduledFlight.EstimatedArrivalDatetime;
        entity.FlightStatusId           = scheduledFlight.FlightStatusId;
        entity.UpdatedAt                = scheduledFlight.UpdatedAt;

        _context.ScheduledFlights.Update(entity);
    }

    public async Task DeleteAsync(
        ScheduledFlightId id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.ScheduledFlights
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ScheduledFlightEntity with id {id.Value} not found.");

        _context.ScheduledFlights.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/ScheduledFlight/UI/ScheduledFlightConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ScheduledFlight.Application.Interfaces;

public sealed class ScheduledFlightConsoleUI
{
    private readonly IScheduledFlightService _service;

    public ScheduledFlightConsoleUI(IScheduledFlightService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== SCHEDULED FLIGHT MODULE ==========");
            Console.WriteLine("1. List all scheduled flights");
            Console.WriteLine("2. Get scheduled flight by ID");
            Console.WriteLine("3. List by base flight");
            Console.WriteLine("4. List by departure date");
            Console.WriteLine("5. Create scheduled flight");
            Console.WriteLine("6. Update scheduled flight");
            Console.WriteLine("7. Delete scheduled flight");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByBaseFlightAsync(); break;
                case "4": await ListByDateAsync();       break;
                case "5": await CreateAsync();           break;
                case "6": await UpdateAsync();           break;
                case "7": await DeleteAsync();           break;
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
        var flights = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Scheduled Flights ---");

        foreach (var f in flights)
            PrintFlight(f);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter scheduled flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var flight = await _service.GetByIdAsync(id);

        if (flight is null)
            Console.WriteLine($"Scheduled flight with ID {id} not found.");
        else
            PrintFlight(flight);
    }

    private async Task ListByBaseFlightAsync()
    {
        Console.Write("Enter Base Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int baseFlightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var flights = await _service.GetByBaseFlightAsync(baseFlightId);
        Console.WriteLine($"\n--- Scheduled Flights for Base Flight {baseFlightId} ---");

        foreach (var f in flights)
            PrintFlight(f);
    }

    private async Task ListByDateAsync()
    {
        Console.Write("Enter departure date (yyyy-MM-dd): ");
        if (!DateOnly.TryParse(Console.ReadLine()?.Trim(), out DateOnly date))
        {
            Console.WriteLine("Invalid date format. Use yyyy-MM-dd.");
            return;
        }

        var flights = await _service.GetByDateAsync(date);
        Console.WriteLine($"\n--- Scheduled Flights on {date:yyyy-MM-dd} ---");

        foreach (var f in flights)
            PrintFlight(f);
    }

    private async Task CreateAsync()
    {
        Console.Write("Base Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int baseFlightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Aircraft ID: ");
        if (!int.TryParse(Console.ReadLine(), out int aircraftId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Gate ID (leave blank if not assigned): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId   = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("Departure date (yyyy-MM-dd): ");
        if (!DateOnly.TryParse(Console.ReadLine()?.Trim(), out DateOnly depDate))
        { Console.WriteLine("Invalid date."); return; }

        Console.Write("Departure time (HH:mm): ");
        if (!TimeOnly.TryParse(Console.ReadLine()?.Trim(), out TimeOnly depTime))
        { Console.WriteLine("Invalid time."); return; }

        Console.Write("Estimated arrival datetime (yyyy-MM-dd HH:mm): ");
        if (!DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime eta))
        { Console.WriteLine("Invalid datetime."); return; }

        Console.Write("Flight Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        var request = new CreateScheduledFlightRequest(
            baseFlightId, aircraftId, gateId, depDate, depTime, eta, statusId);

        var created = await _service.CreateAsync(request);
        Console.WriteLine($"Scheduled flight created: [{created.Id}] " +
                          $"{created.DepartureDate:yyyy-MM-dd} {created.DepartureTime:HH:mm}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Scheduled flight ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Aircraft ID: ");
        if (!int.TryParse(Console.ReadLine(), out int aircraftId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Gate ID (leave blank to clear): ");
        var gateInput = Console.ReadLine()?.Trim();
        int? gateId   = int.TryParse(gateInput, out int gParsed) ? gParsed : null;

        Console.Write("New departure date (yyyy-MM-dd): ");
        if (!DateOnly.TryParse(Console.ReadLine()?.Trim(), out DateOnly depDate))
        { Console.WriteLine("Invalid date."); return; }

        Console.Write("New departure time (HH:mm): ");
        if (!TimeOnly.TryParse(Console.ReadLine()?.Trim(), out TimeOnly depTime))
        { Console.WriteLine("Invalid time."); return; }

        Console.Write("New estimated arrival (yyyy-MM-dd HH:mm): ");
        if (!DateTime.TryParse(Console.ReadLine()?.Trim(), out DateTime eta))
        { Console.WriteLine("Invalid datetime."); return; }

        Console.Write("New Flight Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        var request = new UpdateScheduledFlightRequest(
            aircraftId, gateId, depDate, depTime, eta, statusId);

        await _service.UpdateAsync(id, request);
        Console.WriteLine("Scheduled flight updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter scheduled flight ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Scheduled flight deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintFlight(ScheduledFlightDto f)
        => Console.WriteLine(
            $"  [{f.Id}] BaseFlight: {f.BaseFlightId} | Aircraft: {f.AircraftId} | " +
            $"Gate: {(f.GateId.HasValue ? f.GateId.ToString() : "N/A")} | " +
            $"Departs: {f.DepartureDate:yyyy-MM-dd} {f.DepartureTime:HH:mm} | " +
            $"ETA: {f.EstimatedArrivalDatetime:yyyy-MM-dd HH:mm} | " +
            $"Status: {f.FlightStatusId}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── ScheduledFlight Module ────────────────────────────────────────────────────
builder.Services.AddScoped<IScheduledFlightRepository, ScheduledFlightRepository>();
builder.Services.AddScoped<CreateScheduledFlightUseCase>();
builder.Services.AddScoped<DeleteScheduledFlightUseCase>();
builder.Services.AddScoped<GetAllScheduledFlightsUseCase>();
builder.Services.AddScoped<GetScheduledFlightByIdUseCase>();
builder.Services.AddScoped<UpdateScheduledFlightUseCase>();
builder.Services.AddScoped<GetScheduledFlightsByBaseFlightUseCase>();
builder.Services.AddScoped<GetScheduledFlightsByDateUseCase>();
builder.Services.AddScoped<IScheduledFlightService, ScheduledFlightService>();
builder.Services.AddScoped<ScheduledFlightConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/ScheduledFlight/
├── Application/
│   ├── Interfaces/
│   │   └── IScheduledFlightService.cs
│   ├── Services/
│   │   └── ScheduledFlightService.cs
│   └── UseCases/
│       ├── CreateScheduledFlightUseCase.cs
│       ├── DeleteScheduledFlightUseCase.cs
│       ├── GetAllScheduledFlightsUseCase.cs
│       ├── GetScheduledFlightByIdUseCase.cs
│       ├── GetScheduledFlightsByBaseFlightUseCase.cs
│       ├── GetScheduledFlightsByDateUseCase.cs
│       └── UpdateScheduledFlightUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── ScheduledFlightAggregate.cs
│   ├── Repositories/
│   │   └── IScheduledFlightRepository.cs
│   └── valueObject/
│       └── ScheduledFlightId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── ScheduledFlightEntity.cs
│   │   └── ScheduledFlightEntityConfiguration.cs
│   └── repository/
│       └── ScheduledFlightRepository.cs
└── UI/
    └── ScheduledFlightConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo ScheduledFlight_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
