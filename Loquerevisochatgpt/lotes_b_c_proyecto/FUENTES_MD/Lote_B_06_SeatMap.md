# Módulo: SeatMap
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap`  
**Raíz de archivos:** `src/Modules/SeatMap/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `seat_map_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `aircraft_type_id` | `INT NOT NULL FK` | `int` | FK → `aircraft_type` |
| `seat_number` | `VARCHAR(10) NOT NULL` | `string` | Ej.: 12A, 1C. Normalizado a mayúsculas |
| `cabin_class_id` | `INT NOT NULL FK` | `int` | FK → `cabin_class` |
| `seat_features` | `VARCHAR(100) NULL` | `string?` | Nullable: WINDOW, AISLE, EXTRA_LEGROOM… |

**UNIQUE:** `(aircraft_type_id, seat_number)` — cada número de asiento es único por tipo de aeronave.  
Sin `created_at` ni `updated_at` en el DDL.  
**4NF:** `(aircraft_type_id, seat_number) → cabin_class_id` — sin MVD independientes.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/SeatMap/Domain/valueObject/SeatMapId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;

public sealed class SeatMapId
{
    public int Value { get; }

    public SeatMapId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("SeatMapId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is SeatMapId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/SeatMap/Domain/aggregate/SeatMapAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;

/// <summary>
/// Asiento estático en el mapa de configuración de un tipo de aeronave.
/// SQL: seat_map.
///
/// Representa la disposición física permanente de asientos de un tipo de avión.
/// No cambia entre vuelos — es la fuente de verdad estructural.
///
/// 4NF: (aircraft_type_id, seat_number) → cabin_class_id.
/// No hay dependencias multivaluadas independientes.
///
/// UNIQUE: (aircraft_type_id, seat_number).
/// seat_number normalizado a mayúsculas (ej.: 12A, 1C, 30F).
/// seat_features es nullable: WINDOW, AISLE, EXTRA_LEGROOM, etc.
/// </summary>
public sealed class SeatMapAggregate
{
    public SeatMapId Id             { get; private set; }
    public int       AircraftTypeId { get; private set; }
    public string    SeatNumber     { get; private set; }
    public int       CabinClassId   { get; private set; }
    public string?   SeatFeatures   { get; private set; }

    private SeatMapAggregate()
    {
        Id         = null!;
        SeatNumber = null!;
    }

    public SeatMapAggregate(
        SeatMapId id,
        int       aircraftTypeId,
        string    seatNumber,
        int       cabinClassId,
        string?   seatFeatures = null)
    {
        if (aircraftTypeId <= 0)
            throw new ArgumentException(
                "AircraftTypeId must be a positive integer.", nameof(aircraftTypeId));

        ValidateSeatNumber(seatNumber);

        if (cabinClassId <= 0)
            throw new ArgumentException(
                "CabinClassId must be a positive integer.", nameof(cabinClassId));

        if (seatFeatures is not null && seatFeatures.Length > 100)
            throw new ArgumentException(
                "SeatFeatures cannot exceed 100 characters.", nameof(seatFeatures));

        Id             = id;
        AircraftTypeId = aircraftTypeId;
        SeatNumber     = seatNumber.Trim().ToUpperInvariant();
        CabinClassId   = cabinClassId;
        SeatFeatures   = seatFeatures?.Trim();
    }

    /// <summary>
    /// Actualiza la clase de cabina y las características del asiento.
    /// AircraftTypeId y SeatNumber forman la clave de negocio y no son modificables.
    /// </summary>
    public void Update(int cabinClassId, string? seatFeatures)
    {
        if (cabinClassId <= 0)
            throw new ArgumentException(
                "CabinClassId must be a positive integer.", nameof(cabinClassId));

        if (seatFeatures is not null && seatFeatures.Length > 100)
            throw new ArgumentException(
                "SeatFeatures cannot exceed 100 characters.", nameof(seatFeatures));

        CabinClassId = cabinClassId;
        SeatFeatures = seatFeatures?.Trim();
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static void ValidateSeatNumber(string seatNumber)
    {
        if (string.IsNullOrWhiteSpace(seatNumber))
            throw new ArgumentException("SeatNumber cannot be empty.", nameof(seatNumber));

        if (seatNumber.Trim().Length > 10)
            throw new ArgumentException(
                "SeatNumber cannot exceed 10 characters.", nameof(seatNumber));
    }
}
```

---

### RUTA: `src/Modules/SeatMap/Domain/Repositories/ISeatMapRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;

public interface ISeatMapRepository
{
    Task<SeatMapAggregate?>             GetByIdAsync(SeatMapId id,                        CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatMapAggregate>> GetAllAsync(                                       CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatMapAggregate>> GetByAircraftTypeAsync(int aircraftTypeId,         CancellationToken cancellationToken = default);
    Task                                AddAsync(SeatMapAggregate seatMap,                 CancellationToken cancellationToken = default);
    Task                                UpdateAsync(SeatMapAggregate seatMap,              CancellationToken cancellationToken = default);
    Task                                DeleteAsync(SeatMapId id,                          CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/SeatMap/Application/Interfaces/ISeatMapService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.Interfaces;

public interface ISeatMapService
{
    Task<SeatMapDto?>             GetByIdAsync(int id,                                                                   CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatMapDto>> GetAllAsync(                                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatMapDto>> GetByAircraftTypeAsync(int aircraftTypeId,                                             CancellationToken cancellationToken = default);
    Task<SeatMapDto>              CreateAsync(int aircraftTypeId, string seatNumber, int cabinClassId, string? seatFeatures, CancellationToken cancellationToken = default);
    Task                          UpdateAsync(int id, int cabinClassId, string? seatFeatures,                            CancellationToken cancellationToken = default);
    Task                          DeleteAsync(int id,                                                                    CancellationToken cancellationToken = default);
}

public sealed record SeatMapDto(
    int     Id,
    int     AircraftTypeId,
    string  SeatNumber,
    int     CabinClassId,
    string? SeatFeatures);
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/CreateSeatMapUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateSeatMapUseCase
{
    private readonly ISeatMapRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public CreateSeatMapUseCase(ISeatMapRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SeatMapAggregate> ExecuteAsync(
        int               aircraftTypeId,
        string            seatNumber,
        int               cabinClassId,
        string?           seatFeatures,
        CancellationToken cancellationToken = default)
    {
        // SeatMapId(1) es placeholder; EF Core asigna el Id real al insertar.
        var seatMap = new SeatMapAggregate(
            new SeatMapId(1),
            aircraftTypeId,
            seatNumber,
            cabinClassId,
            seatFeatures);

        await _repository.AddAsync(seatMap, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return seatMap;
    }
}
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/DeleteSeatMapUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteSeatMapUseCase
{
    private readonly ISeatMapRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public DeleteSeatMapUseCase(ISeatMapRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new SeatMapId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/GetAllSeatMapsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;

public sealed class GetAllSeatMapsUseCase
{
    private readonly ISeatMapRepository _repository;

    public GetAllSeatMapsUseCase(ISeatMapRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SeatMapAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/GetSeatMapByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;

public sealed class GetSeatMapByIdUseCase
{
    private readonly ISeatMapRepository _repository;

    public GetSeatMapByIdUseCase(ISeatMapRepository repository)
    {
        _repository = repository;
    }

    public async Task<SeatMapAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new SeatMapId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/UpdateSeatMapUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza la clase de cabina y/o las características de un asiento.
/// AircraftTypeId y SeatNumber son inmutables (forman la clave de negocio).
/// </summary>
public sealed class UpdateSeatMapUseCase
{
    private readonly ISeatMapRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public UpdateSeatMapUseCase(ISeatMapRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               cabinClassId,
        string?           seatFeatures,
        CancellationToken cancellationToken = default)
    {
        var seatMap = await _repository.GetByIdAsync(new SeatMapId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"SeatMap with id {id} was not found.");

        seatMap.Update(cabinClassId, seatFeatures);
        await _repository.UpdateAsync(seatMap, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/SeatMap/Application/UseCases/GetSeatMapsByAircraftTypeUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;

/// <summary>
/// Obtiene todos los asientos del mapa de un tipo de aeronave.
/// Caso de uso clave para consultar la capacidad y distribución antes
/// de generar instancias de flight_seat para un vuelo concreto.
/// </summary>
public sealed class GetSeatMapsByAircraftTypeUseCase
{
    private readonly ISeatMapRepository _repository;

    public GetSeatMapsByAircraftTypeUseCase(ISeatMapRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SeatMapAggregate>> ExecuteAsync(
        int               aircraftTypeId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByAircraftTypeAsync(aircraftTypeId, cancellationToken);
}
```

---

### RUTA: `src/Modules/SeatMap/Application/Services/SeatMapService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;

public sealed class SeatMapService : ISeatMapService
{
    private readonly CreateSeatMapUseCase              _create;
    private readonly DeleteSeatMapUseCase              _delete;
    private readonly GetAllSeatMapsUseCase             _getAll;
    private readonly GetSeatMapByIdUseCase             _getById;
    private readonly UpdateSeatMapUseCase              _update;
    private readonly GetSeatMapsByAircraftTypeUseCase  _getByAircraftType;

    public SeatMapService(
        CreateSeatMapUseCase             create,
        DeleteSeatMapUseCase             delete,
        GetAllSeatMapsUseCase            getAll,
        GetSeatMapByIdUseCase            getById,
        UpdateSeatMapUseCase             update,
        GetSeatMapsByAircraftTypeUseCase getByAircraftType)
    {
        _create            = create;
        _delete            = delete;
        _getAll            = getAll;
        _getById           = getById;
        _update            = update;
        _getByAircraftType = getByAircraftType;
    }

    public async Task<SeatMapDto> CreateAsync(
        int               aircraftTypeId,
        string            seatNumber,
        int               cabinClassId,
        string?           seatFeatures,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            aircraftTypeId, seatNumber, cabinClassId, seatFeatures, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<SeatMapDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<SeatMapDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int               cabinClassId,
        string?           seatFeatures,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, cabinClassId, seatFeatures, cancellationToken);

    public async Task<IEnumerable<SeatMapDto>> GetByAircraftTypeAsync(
        int               aircraftTypeId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByAircraftType.ExecuteAsync(aircraftTypeId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static SeatMapDto ToDto(SeatMapAggregate agg)
        => new(agg.Id.Value, agg.AircraftTypeId, agg.SeatNumber, agg.CabinClassId, agg.SeatFeatures);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/SeatMap/Infrastructure/entity/SeatMapEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Infrastructure.Entity;

public sealed class SeatMapEntity
{
    public int     Id             { get; set; }
    public int     AircraftTypeId { get; set; }
    public string  SeatNumber     { get; set; } = null!;
    public int     CabinClassId   { get; set; }
    public string? SeatFeatures   { get; set; }
}
```

---

### RUTA: `src/Modules/SeatMap/Infrastructure/entity/SeatMapEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SeatMapEntityConfiguration : IEntityTypeConfiguration<SeatMapEntity>
{
    public void Configure(EntityTypeBuilder<SeatMapEntity> builder)
    {
        builder.ToTable("seat_map");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("seat_map_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.AircraftTypeId)
               .HasColumnName("aircraft_type_id")
               .IsRequired();

        builder.Property(e => e.SeatNumber)
               .HasColumnName("seat_number")
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(e => e.CabinClassId)
               .HasColumnName("cabin_class_id")
               .IsRequired();

        builder.Property(e => e.SeatFeatures)
               .HasColumnName("seat_features")
               .IsRequired(false)
               .HasMaxLength(100);

        // UNIQUE (aircraft_type_id, seat_number) — espejo de uq_seat_map
        builder.HasIndex(e => new { e.AircraftTypeId, e.SeatNumber })
               .IsUnique()
               .HasDatabaseName("uq_seat_map");
    }
}
```

---

### RUTA: `src/Modules/SeatMap/Infrastructure/repository/SeatMapRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class SeatMapRepository : ISeatMapRepository
{
    private readonly AppDbContext _context;

    public SeatMapRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static SeatMapAggregate ToDomain(SeatMapEntity entity)
        => new(
            new SeatMapId(entity.Id),
            entity.AircraftTypeId,
            entity.SeatNumber,
            entity.CabinClassId,
            entity.SeatFeatures);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<SeatMapAggregate?> GetByIdAsync(
        SeatMapId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SeatMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<SeatMapAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.SeatMaps
            .AsNoTracking()
            .OrderBy(e => e.AircraftTypeId)
            .ThenBy(e => e.SeatNumber)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<SeatMapAggregate>> GetByAircraftTypeAsync(
        int               aircraftTypeId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.SeatMaps
            .AsNoTracking()
            .Where(e => e.AircraftTypeId == aircraftTypeId)
            .OrderBy(e => e.SeatNumber)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        SeatMapAggregate  seatMap,
        CancellationToken cancellationToken = default)
    {
        var entity = new SeatMapEntity
        {
            AircraftTypeId = seatMap.AircraftTypeId,
            SeatNumber     = seatMap.SeatNumber,
            CabinClassId   = seatMap.CabinClassId,
            SeatFeatures   = seatMap.SeatFeatures
        };
        await _context.SeatMaps.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        SeatMapAggregate  seatMap,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SeatMaps
            .FirstOrDefaultAsync(e => e.Id == seatMap.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SeatMapEntity with id {seatMap.Id.Value} not found.");

        // AircraftTypeId y SeatNumber son la clave de negocio — no se modifican.
        entity.CabinClassId = seatMap.CabinClassId;
        entity.SeatFeatures = seatMap.SeatFeatures;

        _context.SeatMaps.Update(entity);
    }

    public async Task DeleteAsync(
        SeatMapId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SeatMaps
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SeatMapEntity with id {id.Value} not found.");

        _context.SeatMaps.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/SeatMap/UI/SeatMapConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatMap.Application.Interfaces;

public sealed class SeatMapConsoleUI
{
    private readonly ISeatMapService _service;

    public SeatMapConsoleUI(ISeatMapService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== SEAT MAP MODULE ==========");
            Console.WriteLine("1. List all seat map entries");
            Console.WriteLine("2. Get seat map entry by ID");
            Console.WriteLine("3. List seats by aircraft type");
            Console.WriteLine("4. Create seat map entry");
            Console.WriteLine("5. Update seat map entry");
            Console.WriteLine("6. Delete seat map entry");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();            break;
                case "2": await GetByIdAsync();            break;
                case "3": await ListByAircraftTypeAsync(); break;
                case "4": await CreateAsync();             break;
                case "5": await UpdateAsync();             break;
                case "6": await DeleteAsync();             break;
                case "0": running = false;                 break;
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
        Console.WriteLine("\n--- All Seat Map Entries ---");

        foreach (var s in seats)
            PrintSeat(s);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter seat map ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var seat = await _service.GetByIdAsync(id);

        if (seat is null)
            Console.WriteLine($"Seat map entry with ID {id} not found.");
        else
            PrintSeat(seat);
    }

    private async Task ListByAircraftTypeAsync()
    {
        Console.Write("Enter Aircraft Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int aircraftTypeId))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var seats = await _service.GetByAircraftTypeAsync(aircraftTypeId);
        Console.WriteLine($"\n--- Seat Map for Aircraft Type {aircraftTypeId} ---");
        Console.WriteLine($"  Total seats: {seats.Count()}");

        foreach (var s in seats)
            PrintSeat(s);
    }

    private async Task CreateAsync()
    {
        Console.Write("Aircraft Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int aircraftTypeId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Seat number (e.g. 12A): ");
        var seatNumber = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(seatNumber))
        { Console.WriteLine("Seat number cannot be empty."); return; }

        Console.Write("Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinClassId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Seat features (optional, e.g. WINDOW — press Enter to skip): ");
        var features = Console.ReadLine()?.Trim();
        string? seatFeatures = string.IsNullOrWhiteSpace(features) ? null : features;

        var created = await _service.CreateAsync(
            aircraftTypeId, seatNumber, cabinClassId, seatFeatures);

        Console.WriteLine($"Seat map entry created: [{created.Id}] {created.SeatNumber}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Seat map entry ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinClassId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New seat features (optional — press Enter to clear): ");
        var features = Console.ReadLine()?.Trim();
        string? seatFeatures = string.IsNullOrWhiteSpace(features) ? null : features;

        await _service.UpdateAsync(id, cabinClassId, seatFeatures);
        Console.WriteLine("Seat map entry updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter seat map entry ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Seat map entry deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintSeat(SeatMapDto s)
        => Console.WriteLine(
            $"  [{s.Id}] Type: {s.AircraftTypeId} | Seat: {s.SeatNumber} | " +
            $"Class: {s.CabinClassId} | Features: {s.SeatFeatures ?? "—"}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── SeatMap Module ────────────────────────────────────────────────────────────
builder.Services.AddScoped<ISeatMapRepository, SeatMapRepository>();
builder.Services.AddScoped<CreateSeatMapUseCase>();
builder.Services.AddScoped<DeleteSeatMapUseCase>();
builder.Services.AddScoped<GetAllSeatMapsUseCase>();
builder.Services.AddScoped<GetSeatMapByIdUseCase>();
builder.Services.AddScoped<UpdateSeatMapUseCase>();
builder.Services.AddScoped<GetSeatMapsByAircraftTypeUseCase>();
builder.Services.AddScoped<ISeatMapService, SeatMapService>();
builder.Services.AddScoped<SeatMapConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/SeatMap/
├── Application/
│   ├── Interfaces/
│   │   └── ISeatMapService.cs
│   ├── Services/
│   │   └── SeatMapService.cs
│   └── UseCases/
│       ├── CreateSeatMapUseCase.cs
│       ├── DeleteSeatMapUseCase.cs
│       ├── GetAllSeatMapsUseCase.cs
│       ├── GetSeatMapByIdUseCase.cs
│       ├── GetSeatMapsByAircraftTypeUseCase.cs
│       └── UpdateSeatMapUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── SeatMapAggregate.cs
│   ├── Repositories/
│   │   └── ISeatMapRepository.cs
│   └── valueObject/
│       └── SeatMapId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── SeatMapEntity.cs
│   │   └── SeatMapEntityConfiguration.cs
│   └── repository/
│       └── SeatMapRepository.cs
└── UI/
    └── SeatMapConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo SeatMap_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
