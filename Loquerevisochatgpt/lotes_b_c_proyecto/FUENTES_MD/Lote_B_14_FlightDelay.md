# Módulo: FlightDelay
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay`  
**Raíz de archivos:** `src/Modules/FlightDelay/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `flight_delay_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-7] |
| `scheduled_flight_id` | `INT NOT NULL FK` | `int` | FK → `scheduled_flight` |
| `delay_reason_id` | `INT NOT NULL FK` | `int` | FK → `delay_reason` |
| `delay_minutes` | `INT NOT NULL` | `int` | CHECK `> 0` — espejado en el dominio |
| `reported_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |

Sin `updated_at` en el DDL — un retraso reportado no se modifica, se elimina y re-registra si hay error.  
**CHECK [IR implícito]:** `delay_minutes > 0` — espejado en constructor y `AdjustDelay()`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/FlightDelay/Domain/valueObject/FlightDelayId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;

public sealed class FlightDelayId
{
    public int Value { get; }

    public FlightDelayId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("FlightDelayId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is FlightDelayId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/FlightDelay/Domain/aggregate/FlightDelayAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;

/// <summary>
/// Registro de un retraso en un vuelo programado.
/// SQL: flight_delay. [NC-7] id renombrado a flight_delay_id.
///
/// Invariante: delay_minutes > 0 (espejo del CHECK SQL).
/// Un vuelo puede tener múltiples registros de retraso (acumulación).
/// reported_at se fija al momento del reporte — no es modificable.
///
/// AdjustDelay(): única mutación válida — corrige minutos de retraso
/// reportados incorrectamente sin borrar el registro.
/// </summary>
public sealed class FlightDelayAggregate
{
    public FlightDelayId Id                { get; private set; }
    public int           ScheduledFlightId { get; private set; }
    public int           DelayReasonId     { get; private set; }
    public int           DelayMinutes      { get; private set; }
    public DateTime      ReportedAt        { get; private set; }

    private FlightDelayAggregate()
    {
        Id = null!;
    }

    public FlightDelayAggregate(
        FlightDelayId id,
        int           scheduledFlightId,
        int           delayReasonId,
        int           delayMinutes,
        DateTime      reportedAt)
    {
        if (scheduledFlightId <= 0)
            throw new ArgumentException(
                "ScheduledFlightId must be a positive integer.", nameof(scheduledFlightId));

        if (delayReasonId <= 0)
            throw new ArgumentException(
                "DelayReasonId must be a positive integer.", nameof(delayReasonId));

        ValidateDelayMinutes(delayMinutes);

        Id                = id;
        ScheduledFlightId = scheduledFlightId;
        DelayReasonId     = delayReasonId;
        DelayMinutes      = delayMinutes;
        ReportedAt        = reportedAt;
    }

    /// <summary>
    /// Corrige los minutos de retraso reportados incorrectamente.
    /// scheduled_flight_id, delay_reason_id y reported_at son inmutables.
    /// </summary>
    public void AdjustDelay(int delayMinutes)
    {
        ValidateDelayMinutes(delayMinutes);
        DelayMinutes = delayMinutes;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateDelayMinutes(int minutes)
    {
        if (minutes <= 0)
            throw new ArgumentException(
                "DelayMinutes must be greater than 0.", nameof(minutes));
    }
}
```

---

### RUTA: `src/Modules/FlightDelay/Domain/Repositories/IFlightDelayRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;

public interface IFlightDelayRepository
{
    Task<FlightDelayAggregate?>             GetByIdAsync(FlightDelayId id,                    CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightDelayAggregate>> GetAllAsync(                                       CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightDelayAggregate>> GetByFlightAsync(int scheduledFlightId,            CancellationToken cancellationToken = default);
    Task                                    AddAsync(FlightDelayAggregate flightDelay,         CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(FlightDelayAggregate flightDelay,      CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(FlightDelayId id,                      CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/FlightDelay/Application/Interfaces/IFlightDelayService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.Interfaces;

public interface IFlightDelayService
{
    Task<FlightDelayDto?>             GetByIdAsync(int id,                                                          CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightDelayDto>> GetAllAsync(                                                                  CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightDelayDto>> GetByFlightAsync(int scheduledFlightId,                                       CancellationToken cancellationToken = default);
    Task<FlightDelayDto>              CreateAsync(int scheduledFlightId, int delayReasonId, int delayMinutes,       CancellationToken cancellationToken = default);
    Task                              AdjustDelayAsync(int id, int delayMinutes,                                    CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                                                           CancellationToken cancellationToken = default);
}

public sealed record FlightDelayDto(
    int      Id,
    int      ScheduledFlightId,
    int      DelayReasonId,
    int      DelayMinutes,
    DateTime ReportedAt);
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/CreateFlightDelayUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateFlightDelayUseCase
{
    private readonly IFlightDelayRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateFlightDelayUseCase(IFlightDelayRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlightDelayAggregate> ExecuteAsync(
        int               scheduledFlightId,
        int               delayReasonId,
        int               delayMinutes,
        CancellationToken cancellationToken = default)
    {
        // FlightDelayId(1) es placeholder; EF Core asigna el Id real al insertar.
        var flightDelay = new FlightDelayAggregate(
            new FlightDelayId(1),
            scheduledFlightId,
            delayReasonId,
            delayMinutes,
            DateTime.UtcNow);

        await _repository.AddAsync(flightDelay, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return flightDelay;
    }
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/DeleteFlightDelayUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteFlightDelayUseCase
{
    private readonly IFlightDelayRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteFlightDelayUseCase(IFlightDelayRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new FlightDelayId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/GetAllFlightDelaysUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;

public sealed class GetAllFlightDelaysUseCase
{
    private readonly IFlightDelayRepository _repository;

    public GetAllFlightDelaysUseCase(IFlightDelayRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightDelayAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/GetFlightDelayByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;

public sealed class GetFlightDelayByIdUseCase
{
    private readonly IFlightDelayRepository _repository;

    public GetFlightDelayByIdUseCase(IFlightDelayRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlightDelayAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new FlightDelayId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/UpdateFlightDelayUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Corrige los minutos de retraso de un registro existente.
/// scheduled_flight_id, delay_reason_id y reported_at son inmutables.
/// </summary>
public sealed class UpdateFlightDelayUseCase
{
    private readonly IFlightDelayRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateFlightDelayUseCase(IFlightDelayRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               delayMinutes,
        CancellationToken cancellationToken = default)
    {
        var flightDelay = await _repository.GetByIdAsync(new FlightDelayId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"FlightDelay with id {id} was not found.");

        flightDelay.AdjustDelay(delayMinutes);
        await _repository.UpdateAsync(flightDelay, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/UseCases/GetFlightDelaysByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;

/// <summary>
/// Obtiene todos los registros de retraso de un vuelo programado.
/// Un vuelo puede acumular múltiples retrasos durante su operación.
/// </summary>
public sealed class GetFlightDelaysByFlightUseCase
{
    private readonly IFlightDelayRepository _repository;

    public GetFlightDelaysByFlightUseCase(IFlightDelayRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightDelayAggregate>> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightDelay/Application/Services/FlightDelayService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;

public sealed class FlightDelayService : IFlightDelayService
{
    private readonly CreateFlightDelayUseCase         _create;
    private readonly DeleteFlightDelayUseCase         _delete;
    private readonly GetAllFlightDelaysUseCase        _getAll;
    private readonly GetFlightDelayByIdUseCase        _getById;
    private readonly UpdateFlightDelayUseCase         _update;
    private readonly GetFlightDelaysByFlightUseCase   _getByFlight;

    public FlightDelayService(
        CreateFlightDelayUseCase       create,
        DeleteFlightDelayUseCase       delete,
        GetAllFlightDelaysUseCase      getAll,
        GetFlightDelayByIdUseCase      getById,
        UpdateFlightDelayUseCase       update,
        GetFlightDelaysByFlightUseCase getByFlight)
    {
        _create      = create;
        _delete      = delete;
        _getAll      = getAll;
        _getById     = getById;
        _update      = update;
        _getByFlight = getByFlight;
    }

    public async Task<FlightDelayDto> CreateAsync(
        int               scheduledFlightId,
        int               delayReasonId,
        int               delayMinutes,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            scheduledFlightId, delayReasonId, delayMinutes, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<FlightDelayDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<FlightDelayDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task AdjustDelayAsync(
        int               id,
        int               delayMinutes,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, delayMinutes, cancellationToken);

    public async Task<IEnumerable<FlightDelayDto>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByFlight.ExecuteAsync(scheduledFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static FlightDelayDto ToDto(FlightDelayAggregate agg)
        => new(
            agg.Id.Value,
            agg.ScheduledFlightId,
            agg.DelayReasonId,
            agg.DelayMinutes,
            agg.ReportedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/FlightDelay/Infrastructure/entity/FlightDelayEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Infrastructure.Entity;

public sealed class FlightDelayEntity
{
    public int      Id                { get; set; }
    public int      ScheduledFlightId { get; set; }
    public int      DelayReasonId     { get; set; }
    public int      DelayMinutes      { get; set; }
    public DateTime ReportedAt        { get; set; }
}
```

---

### RUTA: `src/Modules/FlightDelay/Infrastructure/entity/FlightDelayEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class FlightDelayEntityConfiguration : IEntityTypeConfiguration<FlightDelayEntity>
{
    public void Configure(EntityTypeBuilder<FlightDelayEntity> builder)
    {
        builder.ToTable("flight_delay");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("flight_delay_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ScheduledFlightId)
               .HasColumnName("scheduled_flight_id")
               .IsRequired();

        builder.Property(e => e.DelayReasonId)
               .HasColumnName("delay_reason_id")
               .IsRequired();

        builder.Property(e => e.DelayMinutes)
               .HasColumnName("delay_minutes")
               .IsRequired();

        builder.Property(e => e.ReportedAt)
               .HasColumnName("reported_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

---

### RUTA: `src/Modules/FlightDelay/Infrastructure/repository/FlightDelayRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class FlightDelayRepository : IFlightDelayRepository
{
    private readonly AppDbContext _context;

    public FlightDelayRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static FlightDelayAggregate ToDomain(FlightDelayEntity entity)
        => new(
            new FlightDelayId(entity.Id),
            entity.ScheduledFlightId,
            entity.DelayReasonId,
            entity.DelayMinutes,
            entity.ReportedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<FlightDelayAggregate?> GetByIdAsync(
        FlightDelayId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightDelays
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<FlightDelayAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightDelays
            .AsNoTracking()
            .OrderByDescending(e => e.ReportedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<FlightDelayAggregate>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightDelays
            .AsNoTracking()
            .Where(e => e.ScheduledFlightId == scheduledFlightId)
            .OrderBy(e => e.ReportedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        FlightDelayAggregate flightDelay,
        CancellationToken    cancellationToken = default)
    {
        var entity = new FlightDelayEntity
        {
            ScheduledFlightId = flightDelay.ScheduledFlightId,
            DelayReasonId     = flightDelay.DelayReasonId,
            DelayMinutes      = flightDelay.DelayMinutes,
            ReportedAt        = flightDelay.ReportedAt
        };
        await _context.FlightDelays.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        FlightDelayAggregate flightDelay,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.FlightDelays
            .FirstOrDefaultAsync(e => e.Id == flightDelay.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightDelayEntity with id {flightDelay.Id.Value} not found.");

        // Solo DelayMinutes es mutable — corrección de un error de reporte.
        // ScheduledFlightId, DelayReasonId y ReportedAt son inmutables.
        entity.DelayMinutes = flightDelay.DelayMinutes;

        _context.FlightDelays.Update(entity);
    }

    public async Task DeleteAsync(
        FlightDelayId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightDelays
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightDelayEntity with id {id.Value} not found.");

        _context.FlightDelays.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/FlightDelay/UI/FlightDelayConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightDelay.Application.Interfaces;

public sealed class FlightDelayConsoleUI
{
    private readonly IFlightDelayService _service;

    public FlightDelayConsoleUI(IFlightDelayService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== FLIGHT DELAY MODULE ==========");
            Console.WriteLine("1. List all delay records");
            Console.WriteLine("2. Get delay by ID");
            Console.WriteLine("3. List delays by flight");
            Console.WriteLine("4. Report delay");
            Console.WriteLine("5. Adjust delay minutes");
            Console.WriteLine("6. Delete delay record");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByFlightAsync();     break;
                case "4": await ReportDelayAsync();      break;
                case "5": await AdjustDelayAsync();      break;
                case "6": await DeleteDelayAsync();      break;
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
        var delays = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Flight Delay Records ---");
        foreach (var d in delays) PrintDelay(d);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter delay record ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var delay = await _service.GetByIdAsync(id);
        if (delay is null) Console.WriteLine($"Flight delay with ID {id} not found.");
        else               PrintDelay(delay);
    }

    private async Task ListByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        var delays = await _service.GetByFlightAsync(flightId);
        var list   = delays.ToList();
        Console.WriteLine($"\n--- Delay Records for Flight {flightId} ---");

        int totalMinutes = list.Sum(d => d.DelayMinutes);
        foreach (var d in list) PrintDelay(d);
        Console.WriteLine($"  Total accumulated delay: {totalMinutes} min ({totalMinutes / 60}h {totalMinutes % 60}m)");
    }

    private async Task ReportDelayAsync()
    {
        Console.Write("Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Delay Reason ID: ");
        if (!int.TryParse(Console.ReadLine(), out int reasonId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Delay in minutes (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int minutes))
        { Console.WriteLine("Invalid value."); return; }

        try
        {
            var created = await _service.CreateAsync(flightId, reasonId, minutes);
            Console.WriteLine(
                $"Delay reported: [{created.Id}] Flight {created.ScheduledFlightId} | " +
                $"Reason {created.DelayReasonId} | {created.DelayMinutes} min | " +
                $"Reported: {created.ReportedAt:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task AdjustDelayAsync()
    {
        Console.Write("Delay record ID to adjust: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Corrected delay in minutes (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int minutes))
        { Console.WriteLine("Invalid value."); return; }

        try
        {
            await _service.AdjustDelayAsync(id, minutes);
            Console.WriteLine($"Delay adjusted to {minutes} minutes.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteDelayAsync()
    {
        Console.Write("Delay record ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Delay record deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintDelay(FlightDelayDto d)
        => Console.WriteLine(
            $"  [{d.Id}] Flight: {d.ScheduledFlightId} | " +
            $"Reason: {d.DelayReasonId} | {d.DelayMinutes} min | " +
            $"Reported: {d.ReportedAt:yyyy-MM-dd HH:mm}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── FlightDelay Module ────────────────────────────────────────────────────────
builder.Services.AddScoped<IFlightDelayRepository, FlightDelayRepository>();
builder.Services.AddScoped<CreateFlightDelayUseCase>();
builder.Services.AddScoped<DeleteFlightDelayUseCase>();
builder.Services.AddScoped<GetAllFlightDelaysUseCase>();
builder.Services.AddScoped<GetFlightDelayByIdUseCase>();
builder.Services.AddScoped<UpdateFlightDelayUseCase>();
builder.Services.AddScoped<GetFlightDelaysByFlightUseCase>();
builder.Services.AddScoped<IFlightDelayService, FlightDelayService>();
builder.Services.AddScoped<FlightDelayConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/FlightDelay/
├── Application/
│   ├── Interfaces/
│   │   └── IFlightDelayService.cs
│   ├── Services/
│   │   └── FlightDelayService.cs
│   └── UseCases/
│       ├── CreateFlightDelayUseCase.cs
│       ├── DeleteFlightDelayUseCase.cs
│       ├── GetAllFlightDelaysUseCase.cs
│       ├── GetFlightDelayByIdUseCase.cs
│       ├── GetFlightDelaysByFlightUseCase.cs
│       └── UpdateFlightDelayUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── FlightDelayAggregate.cs
│   ├── Repositories/
│   │   └── IFlightDelayRepository.cs
│   └── valueObject/
│       └── FlightDelayId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── FlightDelayEntity.cs
│   │   └── FlightDelayEntityConfiguration.cs
│   └── repository/
│       └── FlightDelayRepository.cs
└── UI/
    └── FlightDelayConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo FlightDelay_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
