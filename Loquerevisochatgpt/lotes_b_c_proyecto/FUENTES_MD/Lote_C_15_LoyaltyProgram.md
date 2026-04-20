# Módulo: LoyaltyProgram
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram`  
**Raíz de archivos:** `src/Modules/LoyaltyProgram/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `loyalty_program_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-6] |
| `airline_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `airline`. UNIQUE: una aerolínea, un programa |
| `name` | `VARCHAR(100) NOT NULL UNIQUE` | `string` | Nombre del programa (ej. "LifeMiles", "AAdvantage") |
| `miles_per_dollar` | `DECIMAL(6,2) NOT NULL DEFAULT 1` | `decimal` | Millas acumuladas por dólar gastado |

**UNIQUE:** `airline_id` + `name` (independientes en el DDL).  
Sin `created_at`, `updated_at` en el DDL.  
`miles_per_dollar > 0` — validado en dominio (no tiene sentido un programa que no acumule millas).

---

## 1. DOMAIN

---

### RUTA: `src/Modules/LoyaltyProgram/Domain/valueObject/LoyaltyProgramId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;

public sealed class LoyaltyProgramId
{
    public int Value { get; }

    public LoyaltyProgramId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("LoyaltyProgramId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is LoyaltyProgramId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Domain/aggregate/LoyaltyProgramAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;

/// <summary>
/// Programa de fidelización de una aerolínea.
/// SQL: loyalty_program. [NC-6] id renombrado a loyalty_program_id.
///
/// UNIQUE: airline_id — una aerolínea tiene un único programa.
/// UNIQUE: name — el nombre del programa es único globalmente.
///
/// Invariantes:
///   - miles_per_dollar > 0 — un programa que no acumula millas no tiene sentido.
///   - airline_id es inmutable (la aerolínea propietaria no cambia).
///
/// Update(): modifica nombre y tasa de acumulación.
/// </summary>
public sealed class LoyaltyProgramAggregate
{
    public LoyaltyProgramId Id             { get; private set; }
    public int              AirlineId      { get; private set; }
    public string           Name           { get; private set; }
    public decimal          MilesPerDollar { get; private set; }

    private LoyaltyProgramAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public LoyaltyProgramAggregate(
        LoyaltyProgramId id,
        int              airlineId,
        string           name,
        decimal          milesPerDollar)
    {
        if (airlineId <= 0)
            throw new ArgumentException(
                "AirlineId must be a positive integer.", nameof(airlineId));

        ValidateName(name);
        ValidateMilesPerDollar(milesPerDollar);

        Id             = id;
        AirlineId      = airlineId;
        Name           = name.Trim();
        MilesPerDollar = milesPerDollar;
    }

    /// <summary>
    /// Actualiza el nombre y la tasa de acumulación de millas.
    /// airline_id es inmutable — la aerolínea propietaria no cambia.
    /// </summary>
    public void Update(string name, decimal milesPerDollar)
    {
        ValidateName(name);
        ValidateMilesPerDollar(milesPerDollar);

        Name           = name.Trim();
        MilesPerDollar = milesPerDollar;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("LoyaltyProgram name cannot be empty.", nameof(name));

        if (name.Trim().Length > 100)
            throw new ArgumentException(
                "LoyaltyProgram name cannot exceed 100 characters.", nameof(name));
    }

    private static void ValidateMilesPerDollar(decimal milesPerDollar)
    {
        if (milesPerDollar <= 0)
            throw new ArgumentException(
                "MilesPerDollar must be greater than 0.", nameof(milesPerDollar));
    }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Domain/Repositories/ILoyaltyProgramRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;

public interface ILoyaltyProgramRepository
{
    Task<LoyaltyProgramAggregate?>             GetByIdAsync(LoyaltyProgramId id,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyProgramAggregate>> GetAllAsync(                                           CancellationToken cancellationToken = default);
    Task<LoyaltyProgramAggregate?>             GetByAirlineAsync(int airlineId,                       CancellationToken cancellationToken = default);
    Task                                       AddAsync(LoyaltyProgramAggregate loyaltyProgram,       CancellationToken cancellationToken = default);
    Task                                       UpdateAsync(LoyaltyProgramAggregate loyaltyProgram,    CancellationToken cancellationToken = default);
    Task                                       DeleteAsync(LoyaltyProgramId id,                       CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/LoyaltyProgram/Application/Interfaces/ILoyaltyProgramService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.Interfaces;

public interface ILoyaltyProgramService
{
    Task<LoyaltyProgramDto?>             GetByIdAsync(int id,                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyProgramDto>> GetAllAsync(                                                      CancellationToken cancellationToken = default);
    Task<LoyaltyProgramDto?>             GetByAirlineAsync(int airlineId,                                  CancellationToken cancellationToken = default);
    Task<LoyaltyProgramDto>              CreateAsync(int airlineId, string name, decimal milesPerDollar,   CancellationToken cancellationToken = default);
    Task                                 UpdateAsync(int id, string name, decimal milesPerDollar,          CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(int id,                                               CancellationToken cancellationToken = default);
}

public sealed record LoyaltyProgramDto(int Id, int AirlineId, string Name, decimal MilesPerDollar);
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/CreateLoyaltyProgramUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateLoyaltyProgramUseCase
{
    private readonly ILoyaltyProgramRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public CreateLoyaltyProgramUseCase(ILoyaltyProgramRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyProgramAggregate> ExecuteAsync(
        int               airlineId,
        string            name,
        decimal           milesPerDollar,
        CancellationToken cancellationToken = default)
    {
        // LoyaltyProgramId(1) es placeholder; EF Core asigna el Id real al insertar.
        var program = new LoyaltyProgramAggregate(
            new LoyaltyProgramId(1), airlineId, name, milesPerDollar);

        await _repository.AddAsync(program, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return program;
    }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/DeleteLoyaltyProgramUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteLoyaltyProgramUseCase
{
    private readonly ILoyaltyProgramRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public DeleteLoyaltyProgramUseCase(ILoyaltyProgramRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new LoyaltyProgramId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/GetAllLoyaltyProgramsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;

public sealed class GetAllLoyaltyProgramsUseCase
{
    private readonly ILoyaltyProgramRepository _repository;

    public GetAllLoyaltyProgramsUseCase(ILoyaltyProgramRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyProgramAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/GetLoyaltyProgramByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;

public sealed class GetLoyaltyProgramByIdUseCase
{
    private readonly ILoyaltyProgramRepository _repository;

    public GetLoyaltyProgramByIdUseCase(ILoyaltyProgramRepository repository)
    {
        _repository = repository;
    }

    public async Task<LoyaltyProgramAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new LoyaltyProgramId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/UpdateLoyaltyProgramUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateLoyaltyProgramUseCase
{
    private readonly ILoyaltyProgramRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public UpdateLoyaltyProgramUseCase(ILoyaltyProgramRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        decimal           milesPerDollar,
        CancellationToken cancellationToken = default)
    {
        var program = await _repository.GetByIdAsync(new LoyaltyProgramId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"LoyaltyProgram with id {id} was not found.");

        program.Update(name, milesPerDollar);
        await _repository.UpdateAsync(program, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/UseCases/GetLoyaltyProgramByAirlineUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;

/// <summary>
/// Obtiene el programa de fidelización de una aerolínea.
/// La UNIQUE sobre airline_id garantiza como máximo un resultado.
/// </summary>
public sealed class GetLoyaltyProgramByAirlineUseCase
{
    private readonly ILoyaltyProgramRepository _repository;

    public GetLoyaltyProgramByAirlineUseCase(ILoyaltyProgramRepository repository)
    {
        _repository = repository;
    }

    public async Task<LoyaltyProgramAggregate?> ExecuteAsync(
        int               airlineId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByAirlineAsync(airlineId, cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Application/Services/LoyaltyProgramService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.UseCases;

public sealed class LoyaltyProgramService : ILoyaltyProgramService
{
    private readonly CreateLoyaltyProgramUseCase         _create;
    private readonly DeleteLoyaltyProgramUseCase         _delete;
    private readonly GetAllLoyaltyProgramsUseCase        _getAll;
    private readonly GetLoyaltyProgramByIdUseCase        _getById;
    private readonly UpdateLoyaltyProgramUseCase         _update;
    private readonly GetLoyaltyProgramByAirlineUseCase   _getByAirline;

    public LoyaltyProgramService(
        CreateLoyaltyProgramUseCase        create,
        DeleteLoyaltyProgramUseCase        delete,
        GetAllLoyaltyProgramsUseCase       getAll,
        GetLoyaltyProgramByIdUseCase       getById,
        UpdateLoyaltyProgramUseCase        update,
        GetLoyaltyProgramByAirlineUseCase  getByAirline)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _update       = update;
        _getByAirline = getByAirline;
    }

    public async Task<LoyaltyProgramDto> CreateAsync(
        int               airlineId,
        string            name,
        decimal           milesPerDollar,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(airlineId, name, milesPerDollar, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<LoyaltyProgramDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<LoyaltyProgramDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        decimal           milesPerDollar,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, milesPerDollar, cancellationToken);

    public async Task<LoyaltyProgramDto?> GetByAirlineAsync(
        int               airlineId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByAirline.ExecuteAsync(airlineId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    private static LoyaltyProgramDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate.LoyaltyProgramAggregate agg)
        => new(agg.Id.Value, agg.AirlineId, agg.Name, agg.MilesPerDollar);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/LoyaltyProgram/Infrastructure/entity/LoyaltyProgramEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Infrastructure.Entity;

public sealed class LoyaltyProgramEntity
{
    public int     Id             { get; set; }
    public int     AirlineId      { get; set; }
    public string  Name           { get; set; } = null!;
    public decimal MilesPerDollar { get; set; }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Infrastructure/entity/LoyaltyProgramEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class LoyaltyProgramEntityConfiguration : IEntityTypeConfiguration<LoyaltyProgramEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgramEntity> builder)
    {
        builder.ToTable("loyalty_program");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("loyalty_program_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.AirlineId)
               .HasColumnName("airline_id")
               .IsRequired();

        builder.HasIndex(e => e.AirlineId)
               .IsUnique()
               .HasDatabaseName("uq_loyalty_program_airline");

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_loyalty_program_name");

        builder.Property(e => e.MilesPerDollar)
               .HasColumnName("miles_per_dollar")
               .IsRequired()
               .HasColumnType("decimal(6,2)")
               .HasDefaultValue(1m);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyProgram/Infrastructure/repository/LoyaltyProgramRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class LoyaltyProgramRepository : ILoyaltyProgramRepository
{
    private readonly AppDbContext _context;

    public LoyaltyProgramRepository(AppDbContext context)
    {
        _context = context;
    }

    private static LoyaltyProgramAggregate ToDomain(LoyaltyProgramEntity entity)
        => new(new LoyaltyProgramId(entity.Id), entity.AirlineId, entity.Name, entity.MilesPerDollar);

    public async Task<LoyaltyProgramAggregate?> GetByIdAsync(
        LoyaltyProgramId  id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyPrograms
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<LoyaltyProgramAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyPrograms
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<LoyaltyProgramAggregate?> GetByAirlineAsync(
        int               airlineId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyPrograms
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.AirlineId == airlineId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        LoyaltyProgramAggregate loyaltyProgram,
        CancellationToken       cancellationToken = default)
    {
        var entity = new LoyaltyProgramEntity
        {
            AirlineId      = loyaltyProgram.AirlineId,
            Name           = loyaltyProgram.Name,
            MilesPerDollar = loyaltyProgram.MilesPerDollar
        };
        await _context.LoyaltyPrograms.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        LoyaltyProgramAggregate loyaltyProgram,
        CancellationToken       cancellationToken = default)
    {
        var entity = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(e => e.Id == loyaltyProgram.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyProgramEntity with id {loyaltyProgram.Id.Value} not found.");

        // AirlineId es inmutable.
        entity.Name           = loyaltyProgram.Name;
        entity.MilesPerDollar = loyaltyProgram.MilesPerDollar;

        _context.LoyaltyPrograms.Update(entity);
    }

    public async Task DeleteAsync(
        LoyaltyProgramId  id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyPrograms
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyProgramEntity with id {id.Value} not found.");

        _context.LoyaltyPrograms.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/LoyaltyProgram/UI/LoyaltyProgramConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyProgram.Application.Interfaces;

public sealed class LoyaltyProgramConsoleUI
{
    private readonly ILoyaltyProgramService _service;

    public LoyaltyProgramConsoleUI(ILoyaltyProgramService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== LOYALTY PROGRAM MODULE ==========");
            Console.WriteLine("1. List all programs");
            Console.WriteLine("2. Get program by ID");
            Console.WriteLine("3. Get program by airline");
            Console.WriteLine("4. Create program");
            Console.WriteLine("5. Update program");
            Console.WriteLine("6. Delete program");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();         break;
                case "2": await GetByIdAsync();         break;
                case "3": await GetByAirlineAsync();    break;
                case "4": await CreateAsync();          break;
                case "5": await UpdateAsync();          break;
                case "6": await DeleteAsync();          break;
                case "0": running = false;              break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private async Task ListAllAsync()
    {
        var programs = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Loyalty Programs ---");
        foreach (var p in programs)
            Console.WriteLine($"  [{p.Id}] {p.Name} | Airline:{p.AirlineId} | {p.MilesPerDollar:F2} miles/$");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter program ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByIdAsync(id);
        if (p is null) Console.WriteLine($"Program with ID {id} not found.");
        else           Console.WriteLine($"  [{p.Id}] {p.Name} | Airline:{p.AirlineId} | {p.MilesPerDollar:F2} miles/$");
    }

    private async Task GetByAirlineAsync()
    {
        Console.Write("Enter Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId))
        { Console.WriteLine("Invalid ID."); return; }

        var p = await _service.GetByAirlineAsync(airlineId);
        if (p is null) Console.WriteLine($"No loyalty program for airline {airlineId}.");
        else           Console.WriteLine($"  [{p.Id}] {p.Name} | {p.MilesPerDollar:F2} miles/$");
    }

    private async Task CreateAsync()
    {
        Console.Write("Airline ID: ");
        if (!int.TryParse(Console.ReadLine(), out int airlineId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Program name (e.g. LifeMiles): ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Miles per dollar (> 0, default 1): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal mpd) || mpd <= 0)
            mpd = 1m;

        try
        {
            var created = await _service.CreateAsync(airlineId, name, mpd);
            Console.WriteLine($"Program created: [{created.Id}] {created.Name} | {created.MilesPerDollar:F2} miles/$");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Program ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New miles per dollar (> 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal mpd) || mpd <= 0)
        { Console.WriteLine("Invalid value."); return; }

        try
        {
            await _service.UpdateAsync(id, name, mpd);
            Console.WriteLine("Program updated successfully.");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Program ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Program deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento)_

```csharp
// ── LoyaltyProgram Module ─────────────────────────────────────────────────────
builder.Services.AddScoped<ILoyaltyProgramRepository, LoyaltyProgramRepository>();
builder.Services.AddScoped<CreateLoyaltyProgramUseCase>();
builder.Services.AddScoped<DeleteLoyaltyProgramUseCase>();
builder.Services.AddScoped<GetAllLoyaltyProgramsUseCase>();
builder.Services.AddScoped<GetLoyaltyProgramByIdUseCase>();
builder.Services.AddScoped<UpdateLoyaltyProgramUseCase>();
builder.Services.AddScoped<GetLoyaltyProgramByAirlineUseCase>();
builder.Services.AddScoped<ILoyaltyProgramService, LoyaltyProgramService>();
builder.Services.AddScoped<LoyaltyProgramConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/LoyaltyProgram/
├── Application/
│   ├── Interfaces/
│   │   └── ILoyaltyProgramService.cs
│   ├── Services/
│   │   └── LoyaltyProgramService.cs
│   └── UseCases/
│       ├── CreateLoyaltyProgramUseCase.cs
│       ├── DeleteLoyaltyProgramUseCase.cs
│       ├── GetAllLoyaltyProgramsUseCase.cs
│       ├── GetLoyaltyProgramByAirlineUseCase.cs
│       ├── GetLoyaltyProgramByIdUseCase.cs
│       └── UpdateLoyaltyProgramUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── LoyaltyProgramAggregate.cs
│   ├── Repositories/
│   │   └── ILoyaltyProgramRepository.cs
│   └── valueObject/
│       └── LoyaltyProgramId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── LoyaltyProgramEntity.cs
│   │   └── LoyaltyProgramEntityConfiguration.cs
│   └── repository/
│       └── LoyaltyProgramRepository.cs
└── UI/
    └── LoyaltyProgramConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo LoyaltyProgram_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
