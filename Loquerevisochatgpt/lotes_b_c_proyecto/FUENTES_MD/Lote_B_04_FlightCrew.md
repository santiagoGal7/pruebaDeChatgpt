# Módulo: FlightCrew
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew`  
**Raíz de archivos:** `src/Modules/FlightCrew/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `flight_crew_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `scheduled_flight_id` | `INT NOT NULL FK` | `int` | FK → `scheduled_flight` |
| `employee_id` | `INT NOT NULL FK` | `int` | FK → `employee` |
| `crew_role_id` | `INT NOT NULL FK` | `int` | FK → `crew_role` |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |

**UNIQUE:** `(scheduled_flight_id, employee_id)` — un empleado sólo puede tener un rol por vuelo.  
**No tiene `updated_at`:** el DDL no lo incluye. Si se necesita cambiar el rol, se elimina y re-asigna.  
**Nota 4NF:** `(scheduled_flight_id, employee_id) → crew_role_id` — no hay MVD independientes, no viola 4NF.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/FlightCrew/Domain/valueObject/FlightCrewId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;

public sealed class FlightCrewId
{
    public int Value { get; }

    public FlightCrewId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("FlightCrewId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is FlightCrewId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/FlightCrew/Domain/aggregate/FlightCrewAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;

/// <summary>
/// Asignación de un empleado a un vuelo concreto con su rol operativo.
/// SQL: flight_crew.
///
/// 4NF: (scheduled_flight_id, employee_id) → crew_role_id.
/// No hay dependencias multivaluadas independientes — no viola 4NF.
///
/// Invariante clave: UNIQUE (scheduled_flight_id, employee_id).
/// Un empleado sólo puede tener UN rol en un vuelo dado.
///
/// El rol se puede reasignar mediante ReassignRole().
/// No existe updated_at en el DDL; si el negocio lo requiere, se elimina
/// y re-crea la asignación.
/// </summary>
public sealed class FlightCrewAggregate
{
    public FlightCrewId Id                { get; private set; }
    public int          ScheduledFlightId { get; private set; }
    public int          EmployeeId        { get; private set; }
    public int          CrewRoleId        { get; private set; }
    public DateTime     CreatedAt         { get; private set; }

    private FlightCrewAggregate()
    {
        Id = null!;
    }

    public FlightCrewAggregate(
        FlightCrewId id,
        int          scheduledFlightId,
        int          employeeId,
        int          crewRoleId,
        DateTime     createdAt)
    {
        if (scheduledFlightId <= 0)
            throw new ArgumentException(
                "ScheduledFlightId must be a positive integer.", nameof(scheduledFlightId));

        if (employeeId <= 0)
            throw new ArgumentException(
                "EmployeeId must be a positive integer.", nameof(employeeId));

        if (crewRoleId <= 0)
            throw new ArgumentException(
                "CrewRoleId must be a positive integer.", nameof(crewRoleId));

        Id                = id;
        ScheduledFlightId = scheduledFlightId;
        EmployeeId        = employeeId;
        CrewRoleId        = crewRoleId;
        CreatedAt         = createdAt;
    }

    /// <summary>
    /// Reasigna el rol operativo del empleado en este vuelo.
    /// Solo crew_role_id puede cambiar; scheduled_flight_id y employee_id
    /// forman la clave de negocio y no son modificables.
    /// </summary>
    public void ReassignRole(int crewRoleId)
    {
        if (crewRoleId <= 0)
            throw new ArgumentException(
                "CrewRoleId must be a positive integer.", nameof(crewRoleId));

        CrewRoleId = crewRoleId;
    }
}
```

---

### RUTA: `src/Modules/FlightCrew/Domain/Repositories/IFlightCrewRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;

public interface IFlightCrewRepository
{
    Task<FlightCrewAggregate?>             GetByIdAsync(FlightCrewId id,                    CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCrewAggregate>> GetAllAsync(                                     CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCrewAggregate>> GetByFlightAsync(int scheduledFlightId,          CancellationToken cancellationToken = default);
    Task                                   AddAsync(FlightCrewAggregate flightCrew,         CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(FlightCrewAggregate flightCrew,      CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(FlightCrewId id,                     CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/FlightCrew/Application/Interfaces/IFlightCrewService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.Interfaces;

public interface IFlightCrewService
{
    Task<FlightCrewDto?>             GetByIdAsync(int id,                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCrewDto>> GetAllAsync(                                                      CancellationToken cancellationToken = default);
    Task<IEnumerable<FlightCrewDto>> GetByFlightAsync(int scheduledFlightId,                           CancellationToken cancellationToken = default);
    Task<FlightCrewDto>              CreateAsync(int scheduledFlightId, int employeeId, int crewRoleId, CancellationToken cancellationToken = default);
    Task                             UpdateAsync(int id, int crewRoleId,                               CancellationToken cancellationToken = default);
    Task                             DeleteAsync(int id,                                               CancellationToken cancellationToken = default);
}

public sealed record FlightCrewDto(
    int      Id,
    int      ScheduledFlightId,
    int      EmployeeId,
    int      CrewRoleId,
    DateTime CreatedAt);
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/CreateFlightCrewUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateFlightCrewUseCase
{
    private readonly IFlightCrewRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public CreateFlightCrewUseCase(IFlightCrewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlightCrewAggregate> ExecuteAsync(
        int               scheduledFlightId,
        int               employeeId,
        int               crewRoleId,
        CancellationToken cancellationToken = default)
    {
        // FlightCrewId(1) es placeholder; EF Core asigna el Id real al insertar.
        var flightCrew = new FlightCrewAggregate(
            new FlightCrewId(1),
            scheduledFlightId,
            employeeId,
            crewRoleId,
            DateTime.UtcNow);

        await _repository.AddAsync(flightCrew, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return flightCrew;
    }
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/DeleteFlightCrewUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteFlightCrewUseCase
{
    private readonly IFlightCrewRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public DeleteFlightCrewUseCase(IFlightCrewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new FlightCrewId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/GetAllFlightCrewsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;

public sealed class GetAllFlightCrewsUseCase
{
    private readonly IFlightCrewRepository _repository;

    public GetAllFlightCrewsUseCase(IFlightCrewRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightCrewAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/GetFlightCrewByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;

public sealed class GetFlightCrewByIdUseCase
{
    private readonly IFlightCrewRepository _repository;

    public GetFlightCrewByIdUseCase(IFlightCrewRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlightCrewAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new FlightCrewId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/UpdateFlightCrewUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Caso de uso: reasignar el rol operativo de un tripulante en un vuelo.
/// Solo crew_role_id es modificable; los identificadores del vuelo
/// y el empleado forman la clave de negocio y no se alteran.
/// </summary>
public sealed class UpdateFlightCrewUseCase
{
    private readonly IFlightCrewRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public UpdateFlightCrewUseCase(IFlightCrewRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               crewRoleId,
        CancellationToken cancellationToken = default)
    {
        var flightCrew = await _repository.GetByIdAsync(new FlightCrewId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"FlightCrew with id {id} was not found.");

        flightCrew.ReassignRole(crewRoleId);
        await _repository.UpdateAsync(flightCrew, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/UseCases/GetFlightCrewByFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;

/// <summary>
/// Obtiene toda la tripulación asignada a un vuelo programado.
/// Caso de uso clave para la gestión operativa del vuelo.
/// </summary>
public sealed class GetFlightCrewByFlightUseCase
{
    private readonly IFlightCrewRepository _repository;

    public GetFlightCrewByFlightUseCase(IFlightCrewRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<FlightCrewAggregate>> ExecuteAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByFlightAsync(scheduledFlightId, cancellationToken);
}
```

---

### RUTA: `src/Modules/FlightCrew/Application/Services/FlightCrewService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;

public sealed class FlightCrewService : IFlightCrewService
{
    private readonly CreateFlightCrewUseCase      _create;
    private readonly DeleteFlightCrewUseCase      _delete;
    private readonly GetAllFlightCrewsUseCase     _getAll;
    private readonly GetFlightCrewByIdUseCase     _getById;
    private readonly UpdateFlightCrewUseCase      _update;
    private readonly GetFlightCrewByFlightUseCase _getByFlight;

    public FlightCrewService(
        CreateFlightCrewUseCase      create,
        DeleteFlightCrewUseCase      delete,
        GetAllFlightCrewsUseCase     getAll,
        GetFlightCrewByIdUseCase     getById,
        UpdateFlightCrewUseCase      update,
        GetFlightCrewByFlightUseCase getByFlight)
    {
        _create      = create;
        _delete      = delete;
        _getAll      = getAll;
        _getById     = getById;
        _update      = update;
        _getByFlight = getByFlight;
    }

    public async Task<FlightCrewDto> CreateAsync(
        int               scheduledFlightId,
        int               employeeId,
        int               crewRoleId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(scheduledFlightId, employeeId, crewRoleId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<FlightCrewDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<FlightCrewDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int               crewRoleId,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, crewRoleId, cancellationToken);

    public async Task<IEnumerable<FlightCrewDto>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByFlight.ExecuteAsync(scheduledFlightId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static FlightCrewDto ToDto(FlightCrewAggregate agg)
        => new(agg.Id.Value, agg.ScheduledFlightId, agg.EmployeeId, agg.CrewRoleId, agg.CreatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/FlightCrew/Infrastructure/entity/FlightCrewEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Infrastructure.Entity;

public sealed class FlightCrewEntity
{
    public int      Id                { get; set; }
    public int      ScheduledFlightId { get; set; }
    public int      EmployeeId        { get; set; }
    public int      CrewRoleId        { get; set; }
    public DateTime CreatedAt         { get; set; }
}
```

---

### RUTA: `src/Modules/FlightCrew/Infrastructure/entity/FlightCrewEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class FlightCrewEntityConfiguration : IEntityTypeConfiguration<FlightCrewEntity>
{
    public void Configure(EntityTypeBuilder<FlightCrewEntity> builder)
    {
        builder.ToTable("flight_crew");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("flight_crew_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ScheduledFlightId)
               .HasColumnName("scheduled_flight_id")
               .IsRequired();

        builder.Property(e => e.EmployeeId)
               .HasColumnName("employee_id")
               .IsRequired();

        builder.Property(e => e.CrewRoleId)
               .HasColumnName("crew_role_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // UNIQUE (scheduled_flight_id, employee_id) — espejo de uq_fc_employee
        builder.HasIndex(e => new { e.ScheduledFlightId, e.EmployeeId })
               .IsUnique()
               .HasDatabaseName("uq_fc_employee");
    }
}
```

---

### RUTA: `src/Modules/FlightCrew/Infrastructure/repository/FlightCrewRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class FlightCrewRepository : IFlightCrewRepository
{
    private readonly AppDbContext _context;

    public FlightCrewRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static FlightCrewAggregate ToDomain(FlightCrewEntity entity)
        => new(
            new FlightCrewId(entity.Id),
            entity.ScheduledFlightId,
            entity.EmployeeId,
            entity.CrewRoleId,
            entity.CreatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<FlightCrewAggregate?> GetByIdAsync(
        FlightCrewId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightCrews
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<FlightCrewAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightCrews
            .AsNoTracking()
            .OrderBy(e => e.ScheduledFlightId)
            .ThenBy(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<FlightCrewAggregate>> GetByFlightAsync(
        int               scheduledFlightId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.FlightCrews
            .AsNoTracking()
            .Where(e => e.ScheduledFlightId == scheduledFlightId)
            .OrderBy(e => e.CrewRoleId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        FlightCrewAggregate flightCrew,
        CancellationToken   cancellationToken = default)
    {
        var entity = new FlightCrewEntity
        {
            ScheduledFlightId = flightCrew.ScheduledFlightId,
            EmployeeId        = flightCrew.EmployeeId,
            CrewRoleId        = flightCrew.CrewRoleId,
            CreatedAt         = flightCrew.CreatedAt
        };
        await _context.FlightCrews.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        FlightCrewAggregate flightCrew,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.FlightCrews
            .FirstOrDefaultAsync(e => e.Id == flightCrew.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightCrewEntity with id {flightCrew.Id.Value} not found.");

        // Solo crew_role_id es modificable.
        // scheduled_flight_id y employee_id forman la clave de negocio.
        entity.CrewRoleId = flightCrew.CrewRoleId;

        _context.FlightCrews.Update(entity);
    }

    public async Task DeleteAsync(
        FlightCrewId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.FlightCrews
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"FlightCrewEntity with id {id.Value} not found.");

        _context.FlightCrews.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/FlightCrew/UI/FlightCrewConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.FlightCrew.Application.Interfaces;

public sealed class FlightCrewConsoleUI
{
    private readonly IFlightCrewService _service;

    public FlightCrewConsoleUI(IFlightCrewService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== FLIGHT CREW MODULE ==========");
            Console.WriteLine("1. List all crew assignments");
            Console.WriteLine("2. Get assignment by ID");
            Console.WriteLine("3. List crew by flight");
            Console.WriteLine("4. Assign crew member to flight");
            Console.WriteLine("5. Reassign crew role");
            Console.WriteLine("6. Remove crew assignment");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();        break;
                case "2": await GetByIdAsync();        break;
                case "3": await ListByFlightAsync();   break;
                case "4": await AssignAsync();         break;
                case "5": await ReassignRoleAsync();   break;
                case "6": await RemoveAsync();         break;
                case "0": running = false;             break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var crew = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Crew Assignments ---");

        foreach (var c in crew)
            PrintAssignment(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter crew assignment ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var assignment = await _service.GetByIdAsync(id);

        if (assignment is null)
            Console.WriteLine($"Crew assignment with ID {id} not found.");
        else
            PrintAssignment(assignment);
    }

    private async Task ListByFlightAsync()
    {
        Console.Write("Enter Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var crew = await _service.GetByFlightAsync(flightId);
        Console.WriteLine($"\n--- Crew for Scheduled Flight {flightId} ---");

        foreach (var c in crew)
            PrintAssignment(c);
    }

    private async Task AssignAsync()
    {
        Console.Write("Scheduled Flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int flightId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Employee ID: ");
        if (!int.TryParse(Console.ReadLine(), out int employeeId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Crew Role ID: ");
        if (!int.TryParse(Console.ReadLine(), out int roleId))
        { Console.WriteLine("Invalid ID."); return; }

        var created = await _service.CreateAsync(flightId, employeeId, roleId);
        Console.WriteLine(
            $"Crew assigned: [{created.Id}] " +
            $"Employee {created.EmployeeId} → Role {created.CrewRoleId} " +
            $"on Flight {created.ScheduledFlightId}");
    }

    private async Task ReassignRoleAsync()
    {
        Console.Write("Crew assignment ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Crew Role ID: ");
        if (!int.TryParse(Console.ReadLine(), out int roleId))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.UpdateAsync(id, roleId);
        Console.WriteLine("Crew role reassigned successfully.");
    }

    private async Task RemoveAsync()
    {
        Console.Write("Enter crew assignment ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Crew assignment removed successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintAssignment(FlightCrewDto c)
        => Console.WriteLine(
            $"  [{c.Id}] Flight: {c.ScheduledFlightId} | " +
            $"Employee: {c.EmployeeId} | Role: {c.CrewRoleId} | " +
            $"Assigned: {c.CreatedAt:yyyy-MM-dd HH:mm}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── FlightCrew Module ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IFlightCrewRepository, FlightCrewRepository>();
builder.Services.AddScoped<CreateFlightCrewUseCase>();
builder.Services.AddScoped<DeleteFlightCrewUseCase>();
builder.Services.AddScoped<GetAllFlightCrewsUseCase>();
builder.Services.AddScoped<GetFlightCrewByIdUseCase>();
builder.Services.AddScoped<UpdateFlightCrewUseCase>();
builder.Services.AddScoped<GetFlightCrewByFlightUseCase>();
builder.Services.AddScoped<IFlightCrewService, FlightCrewService>();
builder.Services.AddScoped<FlightCrewConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/FlightCrew/
├── Application/
│   ├── Interfaces/
│   │   └── IFlightCrewService.cs
│   ├── Services/
│   │   └── FlightCrewService.cs
│   └── UseCases/
│       ├── CreateFlightCrewUseCase.cs
│       ├── DeleteFlightCrewUseCase.cs
│       ├── GetAllFlightCrewsUseCase.cs
│       ├── GetFlightCrewByIdUseCase.cs
│       ├── GetFlightCrewByFlightUseCase.cs
│       └── UpdateFlightCrewUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── FlightCrewAggregate.cs
│   ├── Repositories/
│   │   └── IFlightCrewRepository.cs
│   └── valueObject/
│       └── FlightCrewId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── FlightCrewEntity.cs
│   │   └── FlightCrewEntityConfiguration.cs
│   └── repository/
│       └── FlightCrewRepository.cs
└── UI/
    └── FlightCrewConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo FlightCrew_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
