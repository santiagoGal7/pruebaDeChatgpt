# Módulo: FlightCancellation
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation`  
**Raíz de archivos:** `src/Modules/FlightCancellation/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `cancellation_id` | `INT AUTO_INCREMENT PK` | `int` | PK nombrado `cancellation_id` (no `flight_cancellation_id`) |
| `scheduled_flight_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `scheduled_flight`. UNIQUE: un vuelo solo puede cancelarse UNA vez |
| `cancellation_reason_id` | `INT NOT NULL FK` | `int` | FK → `cancellation_reason` |
| `cancelled_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create — inmutable |
| `notes` | `VARCHAR(250) NULL` | `string?` | Observaciones adicionales, nullable |

**UNIQUE:** `scheduled_flight_id` — restricción de negocio crítica: un vuelo no puede cancelarse dos veces.  
Sin `updated_at` en el DDL. La única modificación válida es actualizar `notes`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/FlightCancellation/Domain/valueObject/FlightCancellationId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;

public sealed class FlightCancellationId
{
    public int Value { get; }

    public FlightCancellationId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("FlightCancellationId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is FlightCancellationId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/FlightCancellation/Domain/aggregate/FlightCancellationAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;

/// <summary>
/// Registro de cancelación de un vuelo programado.
/// SQL: flight_cancellation. PK: cancellation_id.
///
/// Invariante clave: UNIQUE (scheduled_flight_id) — un vuelo solo puede
/// cancelarse una vez. La unicidad se garantiza a nivel de base de datos
/// y también operativamente: si ya existe un registro para el vuelo,
/// la BD rechazará el INSERT con violación de unique constraint.
///
/// cancelled_at se fija al momento del registro — inmutable.
/// notes es nullable: observaciones opcionales sobre la cancelación.
/// La única modificación válida es actualizar notes (UpdateNotes).
/// </summary>
public sealed class FlightCancellationAggregate
{
    public FlightCancellationId Id                    { get; private set; }
    public int                  ScheduledFlightId     { get; private set; }
    public int                  CancellationReasonId  { get; private set; }
    public DateTime             CancelledAt           { get; private set; }
    public string?              Notes                 { get; private set; }

    private FlightCancellationAggregate()
    {
        Id = null!;
    }

    public FlightCancellationAggregate(
        FlightCancellationId id,
        int                  scheduledFlightId,
        int                  cancellationReasonId,
        DateTime             cancelledAt,
        string?              notes = null)
    {
        if (scheduledFlightId <= 0)
            throw new ArgumentException(
                "ScheduledFlightId must be a positive integer.", nameof(scheduledFlightId));

        if (cancellationReasonId <= 0)
            throw new ArgumentException(
                "CancellationReasonId must be a positive integer.", nameof(cancellationReasonId));

        ValidateNotes(notes);

        Id                   = id;
        ScheduledFlightId    = scheduledFlightId;
        CancellationReasonId = cancellationReasonId;
        CancelledAt          = cancelledAt;
        Notes                = notes?.Trim();
    }

    /// <summary>
    /// Actualiza las notas adicionales de la cancelación.
    /// ScheduledFlightId, CancellationReasonId y CancelledAt son inmutables.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        ValidateNotes(notes);
        Notes = notes?.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateNotes(string? notes)
    {
        if (notes is not null && notes.Trim().Length > 250)
            throw new ArgumentException(
                "Notes cannot exceed 250 characters.", nameof(notes));
    }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Domain/Repositories/IFlightCancellationRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;

public interface IFlightCancellationRepository
{
    Task<FlightCancellationAggregate?>             GetByIdAsync(FlightCancellationId id,                       CancellationToken cancellationToken = default);
    Task<FlightCancellationAggregate?>             GetByFlightAsync(int scheduledFlightId,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCancellationAggregate>> GetAllAsync(                                                 CancellationToken cancellationToken = default);
    Task                                           AddAsync(FlightCancellationAggregate flightCancellation,    CancellationToken cancellationToken = default);
    Task                                           UpdateAsync(FlightCancellationAggregate flightCancellation, CancellationToken cancellationToken = default);
    Task                                           DeleteAsync(FlightCancellationId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/FlightCancellation/Application/Interfaces/IFlightCancellationService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.Interfaces;

public interface IFlightCancellationService
{
    Task<FlightCancellationDto?>             GetByIdAsync(int id,                                                                    CancellationToken cancellationToken = default);
    Task<FlightCancellationDto?>             GetByFlightAsync(int scheduledFlightId,                                                 CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCancellationDto>> GetAllAsync(                                                                            CancellationToken cancellationToken = default);
    Task<FlightCancellationDto>              CreateAsync(int scheduledFlightId, int cancellationReasonId, string? notes,             CancellationToken cancellationToken = default);
    Task                                     UpdateNotesAsync(int id, string? notes,                                                 CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(int id,                                                                     CancellationToken cancellationToken = default);
}

public sealed record FlightCancellationDto(
    int      Id,
    int      ScheduledFlightId,
    int      CancellationReasonId,
    DateTime CancelledAt,
    string?  Notes);
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/CreateFlightCancellationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateFlightCancellationUseCase
{
    private readonly IFlightCancellationRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public CreateFlightCancellationUseCase(IFlightCancellationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlightCancellationAggregate> ExecuteAsync(
        int               scheduledFlightId,
        int               cancellationReasonId,
        string?           notes,
        CancellationToken cancellationToken = default)
    {
        // FlightCancellationId(1) es placeholder; EF Core asigna el Id real al insertar.
        // La UNIQUE constraint sobre scheduled_flight_id garantiza que no se
        // pueda cancelar el mismo vuelo dos veces a nivel de BD.
        var flightCancellation = new FlightCancellationAggregate(
            new FlightCancellationId(1),
            scheduledFlightId,
            cancellationReasonId,
            DateTime.UtcNow,
            notes);

        await _repository.AddAsync(flightCancellation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return flightCancellation;
    }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/DeleteFlightCancellationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteFlightCancellationUseCase
{
    private readonly IFlightCancellationRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public DeleteFlightCancellationUseCase(IFlightCancellationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new FlightCancellationId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/GetAllFlightCancellationsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;

public sealed class GetAllFlightCancellationsUseCase
{
    private readonly IFlightCancellationRepository _repository;

    public GetAllFlightCancellationsUseCase(IFlightCancellationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightCancellationAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/GetFlightCancellationByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;

public sealed class GetFlightCancellationByIdUseCase
{
    private readonly IFlightCancellationRepository _repository;

    public GetFlightCancellationByIdUseCase(IFlightCancellationRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlightCancellationAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new FlightCancellationId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/UpdateFlightCancellationUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza las notas adicionales de una cancelación de vuelo.
/// ScheduledFlightId, CancellationReasonId y CancelledAt son inmutables.
/// </summary>
public sealed class UpdateFlightCancellationUseCase
{
    private readonly IFlightCancellationRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public UpdateFlightCancellationUseCase(IFlightCancellationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string?           notes,
        CancellationToken cancellationToken = default)
    {
        var flightCancellation = await _repository.GetByIdAsync(new FlightCancellationId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"FlightCancellation with id {id} was not found.");

        flightCancellation.UpdateNotes(notes);
        await _repository.UpdateAsync(flightCancellation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/UseCases/GetFlightCancellationByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;

/// <summary>
/// Obtiene el registro de cancelación de un vuelo programado dado.
/// Retorna null si el vuelo no ha sido cancelado.
/// Caso de uso clave para verificar si un vuelo está cancelado antes de
/// procesar reservas o check-in.
/// </summary>
public sealed class GetFlightCancellationByFlightUseCase
{
    private readonly IFlightCancellationRepository _repository;

    public GetFlightCancellationByFlightUseCase(IFlightCancellationRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlightCancellationAggregate?> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCancellation/Application/Services/FlightCancellationService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;

public sealed class FlightCancellationService : IFlightCancellationService
{
    private readonly CreateFlightCancellationUseCase         _create;
    private readonly DeleteFlightCancellationUseCase         _delete;
    private readonly GetAllFlightCancellationsUseCase        _getAll;
    private readonly GetFlightCancellationByIdUseCase        _getById;
    private readonly UpdateFlightCancellationUseCase         _update;
    private readonly GetFlightCancellationByFlightUseCase    _getByFlight;

    public FlightCancellationService(
        CreateFlightCancellationUseCase      create,
        DeleteFlightCancellationUseCase      delete,
        GetAllFlightCancellationsUseCase     getAll,
        GetFlightCancellationByIdUseCase     getById,
        UpdateFlightCancellationUseCase      update,
        GetFlightCancellationByFlightUseCase getByFlight)
    {
        _create      = create;
        _delete      = delete;
        _getAll      = getAll;
        _getById     = getById;
        _update      = update;
        _getByFlight = getByFlight;
    }

    public async Task<FlightCancellationDto> CreateAsync(
        int               scheduledFlightId,
        int               cancellationReasonId,
        string?           notes,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            scheduledFlightId, cancellationReasonId, notes, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<FlightCancellationDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<FlightCancellationDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateNotesAsync(
        int               id,
        string?           notes,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, notes, cancellationToken);

    public async Task<FlightCancellationDto?> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByFlight.ExecuteAsync(scheduledFlightId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static FlightCancellationDto ToDto(FlightCancellationAggregate agg)
        => new(
            agg.Id.Value,
            agg.ScheduledFlightId,
            agg.CancellationReasonId,
            agg.CancelledAt,
            agg.Notes);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/FlightCancellation/Infrastructure/entity/FlightCancellationEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Infrastructure.Entity;

public sealed class FlightCancellationEntity
{
    public int      Id                   { get; set; }
    public int      ScheduledFlightId    { get; set; }
    public int      CancellationReasonId { get; set; }
    public DateTime CancelledAt          { get; set; }
    public string?  Notes                { get; set; }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Infrastructure/entity/FlightCancellationEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class FlightCancellationEntityConfiguration : IEntityTypeConfiguration<FlightCancellationEntity>
{
    public void Configure(EntityTypeBuilder<FlightCancellationEntity> builder)
    {
        builder.ToTable("flight_cancellation");

        builder.HasKey(e => e.Id);

        // PK en SQL es cancellation_id (no flight_cancellation_id)
        builder.Property(e => e.Id)
               .HasColumnName("cancellation_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ScheduledFlightId)
               .HasColumnName("scheduled_flight_id")
               .IsRequired();

        // UNIQUE (scheduled_flight_id) — un vuelo solo puede cancelarse una vez
        builder.HasIndex(e => e.ScheduledFlightId)
               .IsUnique()
               .HasDatabaseName("uq_flight_cancellation_flight");

        builder.Property(e => e.CancellationReasonId)
               .HasColumnName("cancellation_reason_id")
               .IsRequired();

        builder.Property(e => e.CancelledAt)
               .HasColumnName("cancelled_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.Notes)
               .HasColumnName("notes")
               .IsRequired(false)
               .HasMaxLength(250);
    }
}
```

---

### RUTA: `src/Modules/FlightCancellation/Infrastructure/repository/FlightCancellationRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class FlightCancellationRepository : IFlightCancellationRepository
{
    private readonly AppDbContext _context;

    public FlightCancellationRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static FlightCancellationAggregate ToDomain(FlightCancellationEntity entity)
        => new(
            new FlightCancellationId(entity.Id),
            entity.ScheduledFlightId,
            entity.CancellationReasonId,
            entity.CancelledAt,
            entity.Notes);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<FlightCancellationAggregate?> GetByIdAsync(
        FlightCancellationId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.FlightCancellations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<FlightCancellationAggregate?> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        // scheduled_flight_id es UNIQUE — FirstOrDefault es correcto.
        var entity = await _context.FlightCancellations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ScheduledFlightId == scheduledFlightId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<FlightCancellationAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightCancellations
            .AsNoTracking()
            .OrderByDescending(e => e.CancelledAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        FlightCancellationAggregate flightCancellation,
        CancellationToken           cancellationToken = default)
    {
        var entity = new FlightCancellationEntity
        {
            ScheduledFlightId    = flightCancellation.ScheduledFlightId,
            CancellationReasonId = flightCancellation.CancellationReasonId,
            CancelledAt          = flightCancellation.CancelledAt,
            Notes                = flightCancellation.Notes
        };
        await _context.FlightCancellations.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        FlightCancellationAggregate flightCancellation,
        CancellationToken           cancellationToken = default)
    {
        var entity = await _context.FlightCancellations
            .FirstOrDefaultAsync(e => e.Id == flightCancellation.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightCancellationEntity with id {flightCancellation.Id.Value} not found.");

        // Solo Notes es mutable.
        // ScheduledFlightId, CancellationReasonId y CancelledAt son inmutables.
        entity.Notes = flightCancellation.Notes;

        _context.FlightCancellations.Update(entity);
    }

    public async Task DeleteAsync(
        FlightCancellationId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.FlightCancellations
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightCancellationEntity with id {id.Value} not found.");

        _context.FlightCancellations.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/FlightCancellation/UI/FlightCancellationConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCancellation.Application.Interfaces;

public sealed class FlightCancellationConsoleUI
{
    private readonly IFlightCancellationService _service;

    public FlightCancellationConsoleUI(IFlightCancellationService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== FLIGHT CANCELLATION MODULE ==========");
            Console.WriteLine("1. List all cancellations");
            Console.WriteLine("2. Get cancellation by ID");
            Console.WriteLine("3. Check if flight is cancelled");
            Console.WriteLine("4. Register cancellation");
            Console.WriteLine("5. Update notes");
            Console.WriteLine("6. Delete cancellation record");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await CheckByFlightAsync();    break;
                case "4": await RegisterAsync();         break;
                case "5": await UpdateNotesAsync();      break;
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
        var cancellations = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Flight Cancellations ---");
        foreach (var c in cancellations) PrintCancellation(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter cancellation ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByIdAsync(id);
        if (c is null) Console.WriteLine($"Cancellation with ID {id} not found.");
        else           PrintCancellation(c);
    }

    private async Task CheckByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByFlightAsync(flightId);
        if (c is null)
            Console.WriteLine($"Flight {flightId} has NOT been cancelled.");
        else
        {
            Console.WriteLine($"Flight {flightId} IS CANCELLED:");
            PrintCancellation(c);
        }
    }

    private async Task RegisterAsync()
    {
        Console.Write("Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Cancellation Reason ID: ");
        if (!int.TryParse(Console.ReadLine(), out int reasonId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Notes (optional — press Enter to skip): ");
        var notesInput = Console.ReadLine()?.Trim();
        string? notes  = string.IsNullOrWhiteSpace(notesInput) ? null : notesInput;

        try
        {
            var created = await _service.CreateAsync(flightId, reasonId, notes);
            Console.WriteLine(
                $"Cancellation registered: [{created.Id}] Flight {created.ScheduledFlightId} | " +
                $"Reason {created.CancellationReasonId} | " +
                $"Cancelled at: {created.CancelledAt:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateNotesAsync()
    {
        Console.Write("Cancellation ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New notes (press Enter to clear): ");
        var notesInput = Console.ReadLine()?.Trim();
        string? notes  = string.IsNullOrWhiteSpace(notesInput) ? null : notesInput;

        try
        {
            await _service.UpdateNotesAsync(id, notes);
            Console.WriteLine("Notes updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Cancellation ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Cancellation record deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintCancellation(FlightCancellationDto c)
        => Console.WriteLine(
            $"  [{c.Id}] Flight: {c.ScheduledFlightId} | " +
            $"Reason: {c.CancellationReasonId} | " +
            $"Cancelled: {c.CancelledAt:yyyy-MM-dd HH:mm}" +
            (c.Notes is not null ? $" | Notes: {c.Notes}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── FlightCancellation Module ─────────────────────────────────────────────────
builder.Services.AddScoped<IFlightCancellationRepository, FlightCancellationRepository>();
builder.Services.AddScoped<CreateFlightCancellationUseCase>();
builder.Services.AddScoped<DeleteFlightCancellationUseCase>();
builder.Services.AddScoped<GetAllFlightCancellationsUseCase>();
builder.Services.AddScoped<GetFlightCancellationByIdUseCase>();
builder.Services.AddScoped<UpdateFlightCancellationUseCase>();
builder.Services.AddScoped<GetFlightCancellationByFlightUseCase>();
builder.Services.AddScoped<IFlightCancellationService, FlightCancellationService>();
builder.Services.AddScoped<FlightCancellationConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/FlightCancellation/
├── Application/
│   ├── Interfaces/
│   │   └── IFlightCancellationService.cs
│   ├── Services/
│   │   └── FlightCancellationService.cs
│   └── UseCases/
│       ├── CreateFlightCancellationUseCase.cs
│       ├── DeleteFlightCancellationUseCase.cs
│       ├── GetAllFlightCancellationsUseCase.cs
│       ├── GetFlightCancellationByIdUseCase.cs
│       ├── GetFlightCancellationByFlightUseCase.cs
│       └── UpdateFlightCancellationUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── FlightCancellationAggregate.cs
│   ├── Repositories/
│   │   └── IFlightCancellationRepository.cs
│   └── valueObject/
│       └── FlightCancellationId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── FlightCancellationEntity.cs
│   │   └── FlightCancellationEntityConfiguration.cs
│   └── repository/
│       └── FlightCancellationRepository.cs
└── UI/
    └── FlightCancellationConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo FlightCancellation_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
