# Módulo: RouteSchedule
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule`  
**Raíz de archivos:** `src/Modules/RouteSchedule/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `route_schedule_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `base_flight_id` | `INT NOT NULL FK` | `int` | FK → `base_flight` |
| `day_of_week` | `TINYINT NOT NULL` | `byte` | CHECK 1–7 (ISO 8601: 1=Lun … 7=Dom) |
| `departure_time` | `TIME NOT NULL` | `TimeOnly` | .NET 8, Pomelo mapea TIME ↔ TimeOnly nativamente |

**UNIQUE:** `(base_flight_id, day_of_week, departure_time)`  
**No tiene** `created_at` ni `updated_at` en el DDL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/RouteSchedule/Domain/valueObject/RouteScheduleId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;

public sealed class RouteScheduleId
{
    public int Value { get; }

    public RouteScheduleId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("RouteScheduleId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is RouteScheduleId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/RouteSchedule/Domain/aggregate/RouteScheduleAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;

/// <summary>
/// Horario recurrente de operación de un vuelo base [TN-4].
/// Representa en qué día(s) de la semana y a qué hora sale ese vuelo.
/// 
/// Invariantes:
///   - day_of_week: 1 (Lunes) … 7 (Domingo) — ISO 8601 (CHECK SQL).
///   - departure_time: hora válida — representada con TimeOnly (.NET 8).
///   - UNIQUE (base_flight_id, day_of_week, departure_time).
/// </summary>
public sealed class RouteScheduleAggregate
{
    public RouteScheduleId Id            { get; private set; }
    public int             BaseFlightId  { get; private set; }
    public byte            DayOfWeek     { get; private set; }
    public TimeOnly        DepartureTime { get; private set; }

    private RouteScheduleAggregate()
    {
        Id = null!;
    }

    public RouteScheduleAggregate(
        RouteScheduleId id,
        int             baseFlightId,
        byte            dayOfWeek,
        TimeOnly        departureTime)
    {
        if (baseFlightId <= 0)
            throw new ArgumentException("BaseFlightId must be a positive integer.", nameof(baseFlightId));

        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentOutOfRangeException(
                nameof(dayOfWeek), dayOfWeek,
                "DayOfWeek must be between 1 (Monday) and 7 (Sunday) — ISO 8601.");

        Id            = id;
        BaseFlightId  = baseFlightId;
        DayOfWeek     = dayOfWeek;
        DepartureTime = departureTime;
    }

    /// <summary>
    /// Actualiza el horario recurrente.
    /// base_flight_id no se permite cambiar (cambiaría la identidad del vuelo);
    /// solo se actualizan día y hora.
    /// </summary>
    public void Update(byte dayOfWeek, TimeOnly departureTime)
    {
        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentOutOfRangeException(
                nameof(dayOfWeek), dayOfWeek,
                "DayOfWeek must be between 1 (Monday) and 7 (Sunday) — ISO 8601.");

        DayOfWeek     = dayOfWeek;
        DepartureTime = departureTime;
    }

    /// <summary>Nombre legible del día según ISO 8601.</summary>
    public string DayOfWeekName => DayOfWeek switch
    {
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        7 => "Sunday",
        _ => "Unknown"
    };
}
```

---

### RUTA: `src/Modules/RouteSchedule/Domain/Repositories/IRouteScheduleRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;

public interface IRouteScheduleRepository
{
    Task<RouteScheduleAggregate?>             GetByIdAsync(RouteScheduleId id,                   CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteScheduleAggregate>> GetAllAsync(                                        CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteScheduleAggregate>> GetByBaseFlightAsync(int baseFlightId,              CancellationToken cancellationToken = default);
    Task                                      AddAsync(RouteScheduleAggregate routeSchedule,      CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(RouteScheduleAggregate routeSchedule,   CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(RouteScheduleId id,                     CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/RouteSchedule/Application/Interfaces/IRouteScheduleService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.Interfaces;

public interface IRouteScheduleService
{
    Task<RouteScheduleDto?>             GetByIdAsync(int id,                                                   CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteScheduleDto>> GetAllAsync(                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteScheduleDto>> GetByBaseFlightAsync(int baseFlightId,                                 CancellationToken cancellationToken = default);
    Task<RouteScheduleDto>              CreateAsync(int baseFlightId, byte dayOfWeek, TimeOnly departureTime,  CancellationToken cancellationToken = default);
    Task                                UpdateAsync(int id, byte dayOfWeek, TimeOnly departureTime,            CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,                                                    CancellationToken cancellationToken = default);
}

public sealed record RouteScheduleDto(
    int      Id,
    int      BaseFlightId,
    byte     DayOfWeek,
    string   DayOfWeekName,
    TimeOnly DepartureTime);
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/CreateRouteScheduleUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateRouteScheduleUseCase
{
    private readonly IRouteScheduleRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreateRouteScheduleUseCase(IRouteScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RouteScheduleAggregate> ExecuteAsync(
        int             baseFlightId,
        byte            dayOfWeek,
        TimeOnly        departureTime,
        CancellationToken cancellationToken = default)
    {
        // RouteScheduleId(1) es placeholder; EF Core asigna el Id real al insertar.
        var routeSchedule = new RouteScheduleAggregate(
            new RouteScheduleId(1),
            baseFlightId,
            dayOfWeek,
            departureTime);

        await _repository.AddAsync(routeSchedule, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return routeSchedule;
    }
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/DeleteRouteScheduleUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteRouteScheduleUseCase
{
    private readonly IRouteScheduleRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeleteRouteScheduleUseCase(IRouteScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new RouteScheduleId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/GetAllRouteSchedulesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;

public sealed class GetAllRouteSchedulesUseCase
{
    private readonly IRouteScheduleRepository _repository;

    public GetAllRouteSchedulesUseCase(IRouteScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RouteScheduleAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/GetRouteScheduleByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;

public sealed class GetRouteScheduleByIdUseCase
{
    private readonly IRouteScheduleRepository _repository;

    public GetRouteScheduleByIdUseCase(IRouteScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<RouteScheduleAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new RouteScheduleId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/UpdateRouteScheduleUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateRouteScheduleUseCase
{
    private readonly IRouteScheduleRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdateRouteScheduleUseCase(IRouteScheduleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        byte              dayOfWeek,
        TimeOnly          departureTime,
        CancellationToken cancellationToken = default)
    {
        var routeSchedule = await _repository.GetByIdAsync(new RouteScheduleId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"RouteSchedule with id {id} was not found.");

        routeSchedule.Update(dayOfWeek, departureTime);
        await _repository.UpdateAsync(routeSchedule, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/UseCases/GetRouteSchedulesByBaseFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;

/// <summary>
/// Caso de uso adicional: obtiene todos los horarios de un vuelo base dado.
/// Útil para consultar en qué días opera un vuelo antes de generar
/// instancias de scheduled_flight.
/// </summary>
public sealed class GetRouteSchedulesByBaseFlightUseCase
{
    private readonly IRouteScheduleRepository _repository;

    public GetRouteSchedulesByBaseFlightUseCase(IRouteScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RouteScheduleAggregate>> ExecuteAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByBaseFlightAsync(baseFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/RouteSchedule/Application/Services/RouteScheduleService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;

public sealed class RouteScheduleService : IRouteScheduleService
{
    private readonly CreateRouteScheduleUseCase             _create;
    private readonly DeleteRouteScheduleUseCase             _delete;
    private readonly GetAllRouteSchedulesUseCase            _getAll;
    private readonly GetRouteScheduleByIdUseCase            _getById;
    private readonly UpdateRouteScheduleUseCase             _update;
    private readonly GetRouteSchedulesByBaseFlightUseCase   _getByBaseFlight;

    public RouteScheduleService(
        CreateRouteScheduleUseCase           create,
        DeleteRouteScheduleUseCase           delete,
        GetAllRouteSchedulesUseCase          getAll,
        GetRouteScheduleByIdUseCase          getById,
        UpdateRouteScheduleUseCase           update,
        GetRouteSchedulesByBaseFlightUseCase getByBaseFlight)
    {
        _create          = create;
        _delete          = delete;
        _getAll          = getAll;
        _getById         = getById;
        _update          = update;
        _getByBaseFlight = getByBaseFlight;
    }

    public async Task<RouteScheduleDto> CreateAsync(
        int               baseFlightId,
        byte              dayOfWeek,
        TimeOnly          departureTime,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(baseFlightId, dayOfWeek, departureTime, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<RouteScheduleDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<RouteScheduleDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        byte              dayOfWeek,
        TimeOnly          departureTime,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, dayOfWeek, departureTime, cancellationToken);

    public async Task<IEnumerable<RouteScheduleDto>> GetByBaseFlightAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByBaseFlight.ExecuteAsync(baseFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static RouteScheduleDto ToDto(RouteScheduleAggregate agg)
        => new(agg.Id.Value, agg.BaseFlightId, agg.DayOfWeek, agg.DayOfWeekName, agg.DepartureTime);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/RouteSchedule/Infrastructure/entity/RouteScheduleEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Infrastructure.Entity;

public sealed class RouteScheduleEntity
{
    public int      Id            { get; set; }
    public int      BaseFlightId  { get; set; }
    public byte     DayOfWeek     { get; set; }
    public TimeOnly DepartureTime { get; set; }
}
```

---

### RUTA: `src/Modules/RouteSchedule/Infrastructure/entity/RouteScheduleEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RouteScheduleEntityConfiguration : IEntityTypeConfiguration<RouteScheduleEntity>
{
    public void Configure(EntityTypeBuilder<RouteScheduleEntity> builder)
    {
        builder.ToTable("route_schedule");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("route_schedule_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.BaseFlightId)
               .HasColumnName("base_flight_id")
               .IsRequired();

        builder.Property(e => e.DayOfWeek)
               .HasColumnName("day_of_week")
               .IsRequired();

        // Pomelo 8.x mapea TimeOnly ↔ TIME de MySQL de forma nativa.
        builder.Property(e => e.DepartureTime)
               .HasColumnName("departure_time")
               .IsRequired();

        // UNIQUE (base_flight_id, day_of_week, departure_time) — espejo de uq_rs
        builder.HasIndex(e => new { e.BaseFlightId, e.DayOfWeek, e.DepartureTime })
               .IsUnique()
               .HasDatabaseName("uq_rs");
    }
}
```

---

### RUTA: `src/Modules/RouteSchedule/Infrastructure/repository/RouteScheduleRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class RouteScheduleRepository : IRouteScheduleRepository
{
    private readonly AppDbContext _context;

    public RouteScheduleRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static RouteScheduleAggregate ToDomain(RouteScheduleEntity entity)
        => new(
            new RouteScheduleId(entity.Id),
            entity.BaseFlightId,
            entity.DayOfWeek,
            entity.DepartureTime);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<RouteScheduleAggregate?> GetByIdAsync(
        RouteScheduleId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RouteSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<RouteScheduleAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.RouteSchedules
            .AsNoTracking()
            .OrderBy(e => e.BaseFlightId)
            .ThenBy(e => e.DayOfWeek)
            .ThenBy(e => e.DepartureTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<RouteScheduleAggregate>> GetByBaseFlightAsync(
        int               baseFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.RouteSchedules
            .AsNoTracking()
            .Where(e => e.BaseFlightId == baseFlightId)
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.DepartureTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        RouteScheduleAggregate routeSchedule,
        CancellationToken      cancellationToken = default)
    {
        var entity = new RouteScheduleEntity
        {
            BaseFlightId  = routeSchedule.BaseFlightId,
            DayOfWeek     = routeSchedule.DayOfWeek,
            DepartureTime = routeSchedule.DepartureTime
        };
        await _context.RouteSchedules.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        RouteScheduleAggregate routeSchedule,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.RouteSchedules
            .FirstOrDefaultAsync(e => e.Id == routeSchedule.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RouteScheduleEntity with id {routeSchedule.Id.Value} not found.");

        entity.DayOfWeek     = routeSchedule.DayOfWeek;
        entity.DepartureTime = routeSchedule.DepartureTime;

        _context.RouteSchedules.Update(entity);
    }

    public async Task DeleteAsync(
        RouteScheduleId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RouteSchedules
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RouteScheduleEntity with id {id.Value} not found.");

        _context.RouteSchedules.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/RouteSchedule/UI/RouteScheduleConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RouteSchedule.Application.Interfaces;

public sealed class RouteScheduleConsoleUI
{
    private readonly IRouteScheduleService _service;

    public RouteScheduleConsoleUI(IRouteScheduleService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== ROUTE SCHEDULE MODULE ==========");
            Console.WriteLine("1. List all schedules");
            Console.WriteLine("2. Get schedule by ID");
            Console.WriteLine("3. List schedules by base flight");
            Console.WriteLine("4. Create schedule");
            Console.WriteLine("5. Update schedule");
            Console.WriteLine("6. Delete schedule");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByBaseFlightAsync(); break;
                case "4": await CreateAsync();           break;
                case "5": await UpdateAsync();           break;
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
        var schedules = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Route Schedules ---");

        foreach (var s in schedules)
            Console.WriteLine(
                $"  [{s.Id}] FlightId: {s.BaseFlightId} | " +
                $"{s.DayOfWeekName} ({s.DayOfWeek}) | " +
                $"Departs: {s.DepartureTime:HH:mm}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter schedule ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var schedule = await _service.GetByIdAsync(id);

        if (schedule is null)
            Console.WriteLine($"Route schedule with ID {id} not found.");
        else
            Console.WriteLine(
                $"  [{schedule.Id}] FlightId: {schedule.BaseFlightId} | " +
                $"{schedule.DayOfWeekName} ({schedule.DayOfWeek}) | " +
                $"Departs: {schedule.DepartureTime:HH:mm}");
    }

    private async Task ListByBaseFlightAsync()
    {
        Console.Write("Enter Base Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int baseFlightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var schedules = await _service.GetByBaseFlightAsync(baseFlightId);
        Console.WriteLine($"\n--- Schedules for Base Flight {baseFlightId} ---");

        foreach (var s in schedules)
            Console.WriteLine(
                $"  [{s.Id}] {s.DayOfWeekName} ({s.DayOfWeek}) | Departs: {s.DepartureTime:HH:mm}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter Base Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int baseFlightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter day of week (1=Mon, 2=Tue, 3=Wed, 4=Thu, 5=Fri, 6=Sat, 7=Sun): ");
        if (!byte.TryParse(Console.ReadLine(), out byte dayOfWeek) || dayOfWeek < 1 || dayOfWeek > 7)
        {
            Console.WriteLine("Invalid day. Must be between 1 and 7.");
            return;
        }

        Console.Write("Enter departure time (HH:mm, e.g. 14:30): ");
        if (!TimeOnly.TryParse(Console.ReadLine()?.Trim(), out TimeOnly departureTime))
        {
            Console.WriteLine("Invalid time format. Use HH:mm.");
            return;
        }

        var created = await _service.CreateAsync(baseFlightId, dayOfWeek, departureTime);
        Console.WriteLine(
            $"Route schedule created: [{created.Id}] " +
            $"{created.DayOfWeekName} at {created.DepartureTime:HH:mm}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter schedule ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new day of week (1=Mon … 7=Sun): ");
        if (!byte.TryParse(Console.ReadLine(), out byte dayOfWeek) || dayOfWeek < 1 || dayOfWeek > 7)
        {
            Console.WriteLine("Invalid day. Must be between 1 and 7.");
            return;
        }

        Console.Write("Enter new departure time (HH:mm): ");
        if (!TimeOnly.TryParse(Console.ReadLine()?.Trim(), out TimeOnly departureTime))
        {
            Console.WriteLine("Invalid time format. Use HH:mm.");
            return;
        }

        await _service.UpdateAsync(id, dayOfWeek, departureTime);
        Console.WriteLine("Route schedule updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter schedule ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Route schedule deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── RouteSchedule Module ──────────────────────────────────────────────────────
builder.Services.AddScoped<IRouteScheduleRepository, RouteScheduleRepository>();
builder.Services.AddScoped<CreateRouteScheduleUseCase>();
builder.Services.AddScoped<DeleteRouteScheduleUseCase>();
builder.Services.AddScoped<GetAllRouteSchedulesUseCase>();
builder.Services.AddScoped<GetRouteScheduleByIdUseCase>();
builder.Services.AddScoped<UpdateRouteScheduleUseCase>();
builder.Services.AddScoped<GetRouteSchedulesByBaseFlightUseCase>();
builder.Services.AddScoped<IRouteScheduleService, RouteScheduleService>();
builder.Services.AddScoped<RouteScheduleConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/RouteSchedule/
├── Application/
│   ├── Interfaces/
│   │   └── IRouteScheduleService.cs
│   ├── Services/
│   │   └── RouteScheduleService.cs
│   └── UseCases/
│       ├── CreateRouteScheduleUseCase.cs
│       ├── DeleteRouteScheduleUseCase.cs
│       ├── GetAllRouteSchedulesUseCase.cs
│       ├── GetRouteScheduleByIdUseCase.cs
│       ├── GetRouteSchedulesByBaseFlightUseCase.cs
│       └── UpdateRouteScheduleUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── RouteScheduleAggregate.cs
│   ├── Repositories/
│   │   └── IRouteScheduleRepository.cs
│   └── valueObject/
│       └── RouteScheduleId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── RouteScheduleEntity.cs
│   │   └── RouteScheduleEntityConfiguration.cs
│   └── repository/
│       └── RouteScheduleRepository.cs
└── UI/
    └── RouteScheduleConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo RouteSchedule_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
