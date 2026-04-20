# Módulo: BaseFlight
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight`  
**Raíz de archivos:** `src/Modules/BaseFlight/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `base_flight_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `flight_code` | `VARCHAR(20) NOT NULL` | `string` | CHECK `CHAR_LENGTH >= 2` |
| `airline_id` | `INT NOT NULL FK` | `int` | FK → `airline` |
| `route_id` | `INT NOT NULL FK` | `int` | FK → `route` |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura tras Create |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**UNIQUE:** `(flight_code, airline_id)`

---

## 1. DOMAIN

---

### RUTA: `src/Modules/BaseFlight/Domain/valueObject/BaseFlightId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public sealed class BaseFlightId
{
    public int Value { get; }

    public BaseFlightId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BaseFlightId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BaseFlightId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/BaseFlight/Domain/aggregate/BaseFlightAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

/// <summary>
/// Vuelo base: código IATA de vuelo + aerolínea + ruta.
/// Representa la plantilla recurrente sin instancia concreta de fecha/hora.
/// Invariante: flight_code debe tener al menos 2 caracteres (CHECK SQL).
/// UNIQUE: (flight_code, airline_id).
/// </summary>
public sealed class BaseFlightAggregate
{
    public BaseFlightId Id         { get; private set; }
    public string       FlightCode { get; private set; }
    public int          AirlineId  { get; private set; }
    public int          RouteId    { get; private set; }
    public DateTime     CreatedAt  { get; private set; }
    public DateTime?    UpdatedAt  { get; private set; }

    private BaseFlightAggregate()
    {
        Id         = null!;
        FlightCode = null!;
    }

    public BaseFlightAggregate(
        BaseFlightId id,
        string       flightCode,
        int          airlineId,
        int          routeId,
        DateTime     createdAt,
        DateTime?    updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(flightCode))
            throw new ArgumentException("FlightCode cannot be empty.", nameof(flightCode));

        if (flightCode.Trim().Length < 2)
            throw new ArgumentException("FlightCode must be at least 2 characters.", nameof(flightCode));

        if (flightCode.Length > 20)
            throw new ArgumentException("FlightCode cannot exceed 20 characters.", nameof(flightCode));

        if (airlineId <= 0)
            throw new ArgumentException("AirlineId must be a positive integer.", nameof(airlineId));

        if (routeId <= 0)
            throw new ArgumentException("RouteId must be a positive integer.", nameof(routeId));

        Id         = id;
        FlightCode = flightCode.Trim().ToUpperInvariant();
        AirlineId  = airlineId;
        RouteId    = routeId;
        CreatedAt  = createdAt;
        UpdatedAt  = updatedAt;
    }

    /// <summary>
    /// Actualiza los datos modificables del vuelo base.
    /// Registra la fecha de modificación.
    /// </summary>
    public void Update(string flightCode, int airlineId, int routeId)
    {
        if (string.IsNullOrWhiteSpace(flightCode))
            throw new ArgumentException("FlightCode cannot be empty.", nameof(flightCode));

        if (flightCode.Trim().Length < 2)
            throw new ArgumentException("FlightCode must be at least 2 characters.", nameof(flightCode));

        if (flightCode.Length > 20)
            throw new ArgumentException("FlightCode cannot exceed 20 characters.", nameof(flightCode));

        if (airlineId <= 0)
            throw new ArgumentException("AirlineId must be a positive integer.", nameof(airlineId));

        if (routeId <= 0)
            throw new ArgumentException("RouteId must be a positive integer.", nameof(routeId));

        FlightCode = flightCode.Trim().ToUpperInvariant();
        AirlineId  = airlineId;
        RouteId    = routeId;
        UpdatedAt  = DateTime.UtcNow;
    }
}
```

---

### RUTA: `src/Modules/BaseFlight/Domain/Repositories/IBaseFlightRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public interface IBaseFlightRepository
{
    Task<BaseFlightAggregate?>             GetByIdAsync(BaseFlightId id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<BaseFlightAggregate>> GetAllAsync(                               CancellationToken cancellationToken = default);
    Task                                   AddAsync(BaseFlightAggregate baseFlight,   CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(BaseFlightAggregate baseFlight,CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(BaseFlightId id,               CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/BaseFlight/Application/Interfaces/IBaseFlightService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;

public interface IBaseFlightService
{
    Task<BaseFlightDto?>             GetByIdAsync(int id,                                            CancellationToken cancellationToken = default);
    Task<IEnumerable<BaseFlightDto>> GetAllAsync(                                                    CancellationToken cancellationToken = default);
    Task<BaseFlightDto>              CreateAsync(string flightCode, int airlineId, int routeId,      CancellationToken cancellationToken = default);
    Task                             UpdateAsync(int id, string flightCode, int airlineId, int routeId, CancellationToken cancellationToken = default);
    Task                             DeleteAsync(int id,                                             CancellationToken cancellationToken = default);
}

public sealed record BaseFlightDto(
    int      Id,
    string   FlightCode,
    int      AirlineId,
    int      RouteId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/BaseFlight/Application/UseCases/CreateBaseFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public CreateBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseFlightAggregate> ExecuteAsync(
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        // BaseFlightId(1) es placeholder; EF Core asigna el Id real al insertar (ValueGeneratedOnAdd).
        var baseFlight = new BaseFlightAggregate(
            new BaseFlightId(1),
            flightCode,
            airlineId,
            routeId,
            DateTime.UtcNow);

        await _repository.AddAsync(baseFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baseFlight;
    }
}
```

---

### RUTA: `src/Modules/BaseFlight/Application/UseCases/DeleteBaseFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public DeleteBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaseFlightId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaseFlight/Application/UseCases/GetAllBaseFlightsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;

public sealed class GetAllBaseFlightsUseCase
{
    private readonly IBaseFlightRepository _repository;

    public GetAllBaseFlightsUseCase(IBaseFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaseFlightAggregate>> ExecuteAsync(CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/BaseFlight/Application/UseCases/GetBaseFlightByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public sealed class GetBaseFlightByIdUseCase
{
    private readonly IBaseFlightRepository _repository;

    public GetBaseFlightByIdUseCase(IBaseFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseFlightAggregate?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaseFlightId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/BaseFlight/Application/UseCases/UpdateBaseFlightUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public UpdateBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int    id,
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        var baseFlight = await _repository.GetByIdAsync(new BaseFlightId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaseFlight with id {id} was not found.");

        baseFlight.Update(flightCode, airlineId, routeId);
        await _repository.UpdateAsync(baseFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaseFlight/Application/Services/BaseFlightService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

public sealed class BaseFlightService : IBaseFlightService
{
    private readonly CreateBaseFlightUseCase   _create;
    private readonly DeleteBaseFlightUseCase   _delete;
    private readonly GetAllBaseFlightsUseCase  _getAll;
    private readonly GetBaseFlightByIdUseCase  _getById;
    private readonly UpdateBaseFlightUseCase   _update;

    public BaseFlightService(
        CreateBaseFlightUseCase   create,
        DeleteBaseFlightUseCase   delete,
        GetAllBaseFlightsUseCase  getAll,
        GetBaseFlightByIdUseCase  getById,
        UpdateBaseFlightUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<BaseFlightDto> CreateAsync(
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(flightCode, airlineId, routeId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaseFlightDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaseFlightDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int    id,
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, flightCode, airlineId, routeId, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaseFlightDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate.BaseFlightAggregate agg)
        => new(agg.Id.Value, agg.FlightCode, agg.AirlineId, agg.RouteId, agg.CreatedAt, agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/BaseFlight/Infrastructure/entity/BaseFlightEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;

public sealed class BaseFlightEntity
{
    public int       Id         { get; set; }
    public string    FlightCode { get; set; } = null!;
    public int       AirlineId  { get; set; }
    public int       RouteId    { get; set; }
    public DateTime  CreatedAt  { get; set; }
    public DateTime? UpdatedAt  { get; set; }
}
```

---

### RUTA: `src/Modules/BaseFlight/Infrastructure/entity/BaseFlightEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaseFlightEntityConfiguration : IEntityTypeConfiguration<BaseFlightEntity>
{
    public void Configure(EntityTypeBuilder<BaseFlightEntity> builder)
    {
        builder.ToTable("base_flight");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("base_flight_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.FlightCode)
               .HasColumnName("flight_code")
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.AirlineId)
               .HasColumnName("airline_id")
               .IsRequired();

        builder.Property(e => e.RouteId)
               .HasColumnName("route_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);

        // UNIQUE (flight_code, airline_id) — espejo de uq_base_flight
        builder.HasIndex(e => new { e.FlightCode, e.AirlineId })
               .IsUnique()
               .HasDatabaseName("uq_base_flight");
    }
}
```

---

### RUTA: `src/Modules/BaseFlight/Infrastructure/repository/BaseFlightRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaseFlightRepository : IBaseFlightRepository
{
    private readonly AppDbContext _context;

    public BaseFlightRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaseFlightAggregate ToDomain(BaseFlightEntity entity)
        => new(
            new BaseFlightId(entity.Id),
            entity.FlightCode,
            entity.AirlineId,
            entity.RouteId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaseFlightAggregate?> GetByIdAsync(
        BaseFlightId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaseFlightAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaseFlights
            .AsNoTracking()
            .OrderBy(e => e.FlightCode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        BaseFlightAggregate baseFlight,
        CancellationToken   cancellationToken = default)
    {
        // Id lo asigna la BD (AUTO_INCREMENT); no se incluye en la entidad al insertar.
        var entity = new BaseFlightEntity
        {
            FlightCode = baseFlight.FlightCode,
            AirlineId  = baseFlight.AirlineId,
            RouteId    = baseFlight.RouteId,
            CreatedAt  = baseFlight.CreatedAt,
            UpdatedAt  = baseFlight.UpdatedAt
        };
        await _context.BaseFlights.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaseFlightAggregate baseFlight,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .FirstOrDefaultAsync(e => e.Id == baseFlight.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaseFlightEntity with id {baseFlight.Id.Value} not found.");

        entity.FlightCode = baseFlight.FlightCode;
        entity.AirlineId  = baseFlight.AirlineId;
        entity.RouteId    = baseFlight.RouteId;
        entity.UpdatedAt  = baseFlight.UpdatedAt;

        _context.BaseFlights.Update(entity);
    }

    public async Task DeleteAsync(
        BaseFlightId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaseFlightEntity with id {id.Value} not found.");

        _context.BaseFlights.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/BaseFlight/UI/BaseFlightConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.Interfaces;

public sealed class BaseFlightConsoleUI
{
    private readonly IBaseFlightService _service;

    public BaseFlightConsoleUI(IBaseFlightService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BASE FLIGHT MODULE ==========");
            Console.WriteLine("1. List all base flights");
            Console.WriteLine("2. Get base flight by ID");
            Console.WriteLine("3. Create base flight");
            Console.WriteLine("4. Update base flight");
            Console.WriteLine("5. Delete base flight");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();  break;
                case "2": await GetByIdAsync();  break;
                case "3": await CreateAsync();   break;
                case "4": await UpdateAsync();   break;
                case "5": await DeleteAsync();   break;
                case "0": running = false;       break;
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
        Console.WriteLine("\n--- All Base Flights ---");

        foreach (var f in flights)
            Console.WriteLine(
                $"  [{f.Id}] {f.FlightCode} | AirlineId: {f.AirlineId} | RouteId: {f.RouteId} | " +
                $"Created: {f.CreatedAt:yyyy-MM-dd HH:mm}" +
                (f.UpdatedAt.HasValue ? $" | Updated: {f.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter base flight ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var flight = await _service.GetByIdAsync(id);

        if (flight is null)
            Console.WriteLine($"Base flight with ID {id} not found.");
        else
            Console.WriteLine(
                $"  [{flight.Id}] {flight.FlightCode} | " +
                $"AirlineId: {flight.AirlineId} | RouteId: {flight.RouteId} | " +
                $"Created: {flight.CreatedAt:yyyy-MM-dd HH:mm}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter flight code (min 2 chars, e.g. AV101): ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Flight code cannot be empty.");
            return;
        }

        Console.Write("Enter Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId))
        {
            Console.WriteLine("Invalid Airline ID.");
            return;
        }

        Console.Write("Enter Route ID: ");
        if (!int.TryParse(Console.ReadLine(), out int routeId))
        {
            Console.WriteLine("Invalid Route ID.");
            return;
        }

        var created = await _service.CreateAsync(code, airlineId, routeId);
        Console.WriteLine(
            $"Base flight created successfully: [{created.Id}] {created.FlightCode}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter base flight ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new flight code: ");
        var code = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            Console.WriteLine("Flight code cannot be empty.");
            return;
        }

        Console.Write("Enter new Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId))
        {
            Console.WriteLine("Invalid Airline ID.");
            return;
        }

        Console.Write("Enter new Route ID: ");
        if (!int.TryParse(Console.ReadLine(), out int routeId))
        {
            Console.WriteLine("Invalid Route ID.");
            return;
        }

        await _service.UpdateAsync(id, code, airlineId, routeId);
        Console.WriteLine("Base flight updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter base flight ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Base flight deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── BaseFlight Module ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IBaseFlightRepository, BaseFlightRepository>();
builder.Services.AddScoped<CreateBaseFlightUseCase>();
builder.Services.AddScoped<DeleteBaseFlightUseCase>();
builder.Services.AddScoped<GetAllBaseFlightsUseCase>();
builder.Services.AddScoped<GetBaseFlightByIdUseCase>();
builder.Services.AddScoped<UpdateBaseFlightUseCase>();
builder.Services.AddScoped<IBaseFlightService, BaseFlightService>();
builder.Services.AddScoped<BaseFlightConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/BaseFlight/
├── Application/
│   ├── Interfaces/
│   │   └── IBaseFlightService.cs
│   ├── Services/
│   │   └── BaseFlightService.cs
│   └── UseCases/
│       ├── CreateBaseFlightUseCase.cs
│       ├── DeleteBaseFlightUseCase.cs
│       ├── GetAllBaseFlightsUseCase.cs
│       ├── GetBaseFlightByIdUseCase.cs
│       └── UpdateBaseFlightUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── BaseFlightAggregate.cs
│   ├── Repositories/
│   │   └── IBaseFlightRepository.cs
│   └── valueObject/
│       └── BaseFlightId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── BaseFlightEntity.cs
│   │   └── BaseFlightEntityConfiguration.cs
│   └── repository/
│       └── BaseFlightRepository.cs
└── UI/
    └── BaseFlightConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo BaseFlight_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
