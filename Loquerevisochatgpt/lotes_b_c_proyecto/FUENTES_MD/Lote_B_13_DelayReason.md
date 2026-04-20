# Módulo: DelayReason
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason`  
**Raíz de archivos:** `src/Modules/DelayReason/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `delay_reason_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(80) NOT NULL UNIQUE` | `string` | Ej.: WEATHER, TECHNICAL, ATC, CREW, COMMERCIAL |
| `category` | `VARCHAR(50) NOT NULL` | `string` | Agrupación: ej. METEOROLOGICAL, MECHANICAL, OPERATIONAL |

Sin `created_at` ni `updated_at` en el DDL.  
Ambos campos se normalizan a mayúsculas para consistencia con el catálogo.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/DelayReason/Domain/valueObject/DelayReasonId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public sealed class DelayReasonId
{
    public int Value { get; }

    public DelayReasonId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("DelayReasonId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is DelayReasonId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/DelayReason/Domain/aggregate/DelayReasonAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

/// <summary>
/// Catálogo de razones de retraso de vuelos.
/// SQL: delay_reason.
///
/// name: identificador único del motivo (WEATHER, TECHNICAL, ATC, CREW, COMMERCIAL).
/// category: agrupación del motivo (ej.: METEOROLOGICAL, MECHANICAL, OPERATIONAL).
/// Ambos campos se normalizan a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class DelayReasonAggregate
{
    public DelayReasonId Id       { get; private set; }
    public string        Name     { get; private set; }
    public string        Category { get; private set; }

    private DelayReasonAggregate()
    {
        Id       = null!;
        Name     = null!;
        Category = null!;
    }

    public DelayReasonAggregate(DelayReasonId id, string name, string category)
    {
        ValidateName(name);
        ValidateCategory(category);

        Id       = id;
        Name     = name.Trim().ToUpperInvariant();
        Category = category.Trim().ToUpperInvariant();
    }

    public void Update(string name, string category)
    {
        ValidateName(name);
        ValidateCategory(category);

        Name     = name.Trim().ToUpperInvariant();
        Category = category.Trim().ToUpperInvariant();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("DelayReason name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException("DelayReason name cannot exceed 80 characters.", nameof(name));
    }

    private static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("DelayReason category cannot be empty.", nameof(category));

        if (category.Trim().Length > 50)
            throw new ArgumentException("DelayReason category cannot exceed 50 characters.", nameof(category));
    }
}
```

---

### RUTA: `src/Modules/DelayReason/Domain/Repositories/IDelayReasonRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public interface IDelayReasonRepository
{
    Task<DelayReasonAggregate?>             GetByIdAsync(DelayReasonId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<DelayReasonAggregate>> GetAllAsync(                                  CancellationToken cancellationToken = default);
    Task                                    AddAsync(DelayReasonAggregate delayReason,    CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(DelayReasonAggregate delayReason, CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(DelayReasonId id,                 CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/DelayReason/Application/Interfaces/IDelayReasonService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Interfaces;

public interface IDelayReasonService
{
    Task<DelayReasonDto?>             GetByIdAsync(int id,                           CancellationToken cancellationToken = default);
    Task<IEnumerable<DelayReasonDto>> GetAllAsync(                                   CancellationToken cancellationToken = default);
    Task<DelayReasonDto>              CreateAsync(string name, string category,      CancellationToken cancellationToken = default);
    Task                              UpdateAsync(int id, string name, string category, CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                            CancellationToken cancellationToken = default);
}

public sealed record DelayReasonDto(int Id, string Name, string Category);
```

---

### RUTA: `src/Modules/DelayReason/Application/UseCases/CreateDelayReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DelayReasonAggregate> ExecuteAsync(
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        // DelayReasonId(1) es placeholder; EF Core asigna el Id real al insertar.
        var delayReason = new DelayReasonAggregate(new DelayReasonId(1), name, category);

        await _repository.AddAsync(delayReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return delayReason;
    }
}
```

---

### RUTA: `src/Modules/DelayReason/Application/UseCases/DeleteDelayReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new DelayReasonId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/DelayReason/Application/UseCases/GetAllDelayReasonsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;

public sealed class GetAllDelayReasonsUseCase
{
    private readonly IDelayReasonRepository _repository;

    public GetAllDelayReasonsUseCase(IDelayReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DelayReasonAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/DelayReason/Application/UseCases/GetDelayReasonByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public sealed class GetDelayReasonByIdUseCase
{
    private readonly IDelayReasonRepository _repository;

    public GetDelayReasonByIdUseCase(IDelayReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<DelayReasonAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new DelayReasonId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/DelayReason/Application/UseCases/UpdateDelayReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        var delayReason = await _repository.GetByIdAsync(new DelayReasonId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"DelayReason with id {id} was not found.");

        delayReason.Update(name, category);
        await _repository.UpdateAsync(delayReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/DelayReason/Application/Services/DelayReasonService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

public sealed class DelayReasonService : IDelayReasonService
{
    private readonly CreateDelayReasonUseCase   _create;
    private readonly DeleteDelayReasonUseCase   _delete;
    private readonly GetAllDelayReasonsUseCase  _getAll;
    private readonly GetDelayReasonByIdUseCase  _getById;
    private readonly UpdateDelayReasonUseCase   _update;

    public DelayReasonService(
        CreateDelayReasonUseCase  create,
        DeleteDelayReasonUseCase  delete,
        GetAllDelayReasonsUseCase getAll,
        GetDelayReasonByIdUseCase getById,
        UpdateDelayReasonUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<DelayReasonDto> CreateAsync(
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, category, cancellationToken);
        return new DelayReasonDto(agg.Id.Value, agg.Name, agg.Category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<DelayReasonDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new DelayReasonDto(a.Id.Value, a.Name, a.Category));
    }

    public async Task<DelayReasonDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new DelayReasonDto(agg.Id.Value, agg.Name, agg.Category);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, category, cancellationToken);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/DelayReason/Infrastructure/entity/DelayReasonEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Infrastructure.Entity;

public sealed class DelayReasonEntity
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = null!;
    public string Category { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/DelayReason/Infrastructure/entity/DelayReasonEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class DelayReasonEntityConfiguration : IEntityTypeConfiguration<DelayReasonEntity>
{
    public void Configure(EntityTypeBuilder<DelayReasonEntity> builder)
    {
        builder.ToTable("delay_reason");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("delay_reason_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_delay_reason_name");

        builder.Property(e => e.Category)
               .HasColumnName("category")
               .IsRequired()
               .HasMaxLength(50);
    }
}
```

---

### RUTA: `src/Modules/DelayReason/Infrastructure/repository/DelayReasonRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class DelayReasonRepository : IDelayReasonRepository
{
    private readonly AppDbContext _context;

    public DelayReasonRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static DelayReasonAggregate ToDomain(DelayReasonEntity entity)
        => new(new DelayReasonId(entity.Id), entity.Name, entity.Category);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<DelayReasonAggregate?> GetByIdAsync(
        DelayReasonId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DelayReasons
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<DelayReasonAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.DelayReasons
            .AsNoTracking()
            .OrderBy(e => e.Category)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        DelayReasonAggregate delayReason,
        CancellationToken    cancellationToken = default)
    {
        var entity = new DelayReasonEntity
        {
            Name     = delayReason.Name,
            Category = delayReason.Category
        };
        await _context.DelayReasons.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        DelayReasonAggregate delayReason,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.DelayReasons
            .FirstOrDefaultAsync(e => e.Id == delayReason.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"DelayReasonEntity with id {delayReason.Id.Value} not found.");

        entity.Name     = delayReason.Name;
        entity.Category = delayReason.Category;

        _context.DelayReasons.Update(entity);
    }

    public async Task DeleteAsync(
        DelayReasonId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.DelayReasons
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"DelayReasonEntity with id {id.Value} not found.");

        _context.DelayReasons.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/DelayReason/UI/DelayReasonConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.Interfaces;

public sealed class DelayReasonConsoleUI
{
    private readonly IDelayReasonService _service;

    public DelayReasonConsoleUI(IDelayReasonService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== DELAY REASON MODULE ==========");
            Console.WriteLine("1. List all delay reasons");
            Console.WriteLine("2. Get delay reason by ID");
            Console.WriteLine("3. Create delay reason");
            Console.WriteLine("4. Update delay reason");
            Console.WriteLine("5. Delete delay reason");
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
        var reasons = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Delay Reasons ---");

        foreach (var r in reasons)
            Console.WriteLine($"  [{r.Id}] [{r.Category}] {r.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter delay reason ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var reason = await _service.GetByIdAsync(id);

        if (reason is null)
            Console.WriteLine($"Delay reason with ID {id} not found.");
        else
            Console.WriteLine($"  [{reason.Id}] [{reason.Category}] {reason.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Name (e.g. WEATHER, TECHNICAL, ATC, CREW, COMMERCIAL): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        Console.Write("Category (e.g. METEOROLOGICAL, MECHANICAL, OPERATIONAL): ");
        var category = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(category))
        {
            Console.WriteLine("Category cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name, category);
        Console.WriteLine($"Delay reason created: [{created.Id}] [{created.Category}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter delay reason ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        Console.Write("New category: ");
        var category = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(category))
        {
            Console.WriteLine("Category cannot be empty.");
            return;
        }

        await _service.UpdateAsync(id, name, category);
        Console.WriteLine("Delay reason updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter delay reason ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Delay reason deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── DelayReason Module ────────────────────────────────────────────────────────
builder.Services.AddScoped<IDelayReasonRepository, DelayReasonRepository>();
builder.Services.AddScoped<CreateDelayReasonUseCase>();
builder.Services.AddScoped<DeleteDelayReasonUseCase>();
builder.Services.AddScoped<GetAllDelayReasonsUseCase>();
builder.Services.AddScoped<GetDelayReasonByIdUseCase>();
builder.Services.AddScoped<UpdateDelayReasonUseCase>();
builder.Services.AddScoped<IDelayReasonService, DelayReasonService>();
builder.Services.AddScoped<DelayReasonConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/DelayReason/
├── Application/
│   ├── Interfaces/
│   │   └── IDelayReasonService.cs
│   ├── Services/
│   │   └── DelayReasonService.cs
│   └── UseCases/
│       ├── CreateDelayReasonUseCase.cs
│       ├── DeleteDelayReasonUseCase.cs
│       ├── GetAllDelayReasonsUseCase.cs
│       ├── GetDelayReasonByIdUseCase.cs
│       └── UpdateDelayReasonUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── DelayReasonAggregate.cs
│   ├── Repositories/
│   │   └── IDelayReasonRepository.cs
│   └── valueObject/
│       └── DelayReasonId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── DelayReasonEntity.cs
│   │   └── DelayReasonEntityConfiguration.cs
│   └── repository/
│       └── DelayReasonRepository.cs
└── UI/
    └── DelayReasonConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo DelayReason_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
