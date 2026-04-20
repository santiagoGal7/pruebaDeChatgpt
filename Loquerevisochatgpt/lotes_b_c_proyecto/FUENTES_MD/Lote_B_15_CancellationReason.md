# Módulo: CancellationReason
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason`  
**Raíz de archivos:** `src/Modules/CancellationReason/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `cancellation_reason_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(80) NOT NULL UNIQUE` | `string` | Catálogo: WEATHER, TECHNICAL, COMMERCIAL, FORCE_MAJEURE |

Tabla catálogo mínima. Sin `category`, `created_at` ni `updated_at` en el DDL  
(a diferencia de `delay_reason` que sí tiene `category`).  
El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/CancellationReason/Domain/valueObject/CancellationReasonId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public sealed class CancellationReasonId
{
    public int Value { get; }

    public CancellationReasonId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CancellationReasonId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CancellationReasonId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/CancellationReason/Domain/aggregate/CancellationReasonAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

/// <summary>
/// Catálogo de razones de cancelación de vuelos.
/// SQL: cancellation_reason.
///
/// Valores esperados: WEATHER, TECHNICAL, COMMERCIAL, FORCE_MAJEURE.
/// El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.
/// Nota: a diferencia de delay_reason, NO tiene campo category en el DDL.
/// </summary>
public sealed class CancellationReasonAggregate
{
    public CancellationReasonId Id   { get; private set; }
    public string               Name { get; private set; }

    private CancellationReasonAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public CancellationReasonAggregate(CancellationReasonId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CancellationReason name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "CancellationReason name cannot exceed 80 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("CancellationReason name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 80)
            throw new ArgumentException(
                "CancellationReason name cannot exceed 80 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/CancellationReason/Domain/Repositories/ICancellationReasonRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public interface ICancellationReasonRepository
{
    Task<CancellationReasonAggregate?>             GetByIdAsync(CancellationReasonId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<CancellationReasonAggregate>> GetAllAsync(                                                CancellationToken cancellationToken = default);
    Task                                           AddAsync(CancellationReasonAggregate cancellationReason,   CancellationToken cancellationToken = default);
    Task                                           UpdateAsync(CancellationReasonAggregate cancellationReason,CancellationToken cancellationToken = default);
    Task                                           DeleteAsync(CancellationReasonId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/CancellationReason/Application/Interfaces/ICancellationReasonService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;

public interface ICancellationReasonService
{
    Task<CancellationReasonDto?>             GetByIdAsync(int id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<CancellationReasonDto>> GetAllAsync(                      CancellationToken cancellationToken = default);
    Task<CancellationReasonDto>              CreateAsync(string name,          CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(int id, string name,  CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(int id,               CancellationToken cancellationToken = default);
}

public sealed record CancellationReasonDto(int Id, string Name);
```

---

### RUTA: `src/Modules/CancellationReason/Application/UseCases/CreateCancellationReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCancellationReasonUseCase
{
    private readonly ICancellationReasonRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public CreateCancellationReasonUseCase(ICancellationReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CancellationReasonAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // CancellationReasonId(1) es placeholder; EF Core asigna el Id real al insertar.
        var cancellationReason = new CancellationReasonAggregate(new CancellationReasonId(1), name);

        await _repository.AddAsync(cancellationReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return cancellationReason;
    }
}
```

---

### RUTA: `src/Modules/CancellationReason/Application/UseCases/DeleteCancellationReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCancellationReasonUseCase
{
    private readonly ICancellationReasonRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public DeleteCancellationReasonUseCase(ICancellationReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CancellationReasonId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CancellationReason/Application/UseCases/GetAllCancellationReasonsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;

public sealed class GetAllCancellationReasonsUseCase
{
    private readonly ICancellationReasonRepository _repository;

    public GetAllCancellationReasonsUseCase(ICancellationReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CancellationReasonAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/CancellationReason/Application/UseCases/GetCancellationReasonByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public sealed class GetCancellationReasonByIdUseCase
{
    private readonly ICancellationReasonRepository _repository;

    public GetCancellationReasonByIdUseCase(ICancellationReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<CancellationReasonAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CancellationReasonId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/CancellationReason/Application/UseCases/UpdateCancellationReasonUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCancellationReasonUseCase
{
    private readonly ICancellationReasonRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public UpdateCancellationReasonUseCase(ICancellationReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var cancellationReason = await _repository.GetByIdAsync(new CancellationReasonId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CancellationReason with id {id} was not found.");

        cancellationReason.UpdateName(newName);
        await _repository.UpdateAsync(cancellationReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CancellationReason/Application/Services/CancellationReasonService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

public sealed class CancellationReasonService : ICancellationReasonService
{
    private readonly CreateCancellationReasonUseCase   _create;
    private readonly DeleteCancellationReasonUseCase   _delete;
    private readonly GetAllCancellationReasonsUseCase  _getAll;
    private readonly GetCancellationReasonByIdUseCase  _getById;
    private readonly UpdateCancellationReasonUseCase   _update;

    public CancellationReasonService(
        CreateCancellationReasonUseCase  create,
        DeleteCancellationReasonUseCase  delete,
        GetAllCancellationReasonsUseCase getAll,
        GetCancellationReasonByIdUseCase getById,
        UpdateCancellationReasonUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CancellationReasonDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new CancellationReasonDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CancellationReasonDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new CancellationReasonDto(a.Id.Value, a.Name));
    }

    public async Task<CancellationReasonDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new CancellationReasonDto(agg.Id.Value, agg.Name);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, cancellationToken);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/CancellationReason/Infrastructure/entity/CancellationReasonEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Entity;

public sealed class CancellationReasonEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/CancellationReason/Infrastructure/entity/CancellationReasonEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CancellationReasonEntityConfiguration : IEntityTypeConfiguration<CancellationReasonEntity>
{
    public void Configure(EntityTypeBuilder<CancellationReasonEntity> builder)
    {
        builder.ToTable("cancellation_reason");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("cancellation_reason_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_cancellation_reason_name");
    }
}
```

---

### RUTA: `src/Modules/CancellationReason/Infrastructure/repository/CancellationReasonRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CancellationReasonRepository : ICancellationReasonRepository
{
    private readonly AppDbContext _context;

    public CancellationReasonRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CancellationReasonAggregate ToDomain(CancellationReasonEntity entity)
        => new(new CancellationReasonId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CancellationReasonAggregate?> GetByIdAsync(
        CancellationReasonId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CancellationReasonAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.CancellationReasons
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CancellationReasonAggregate cancellationReason,
        CancellationToken           cancellationToken = default)
    {
        var entity = new CancellationReasonEntity { Name = cancellationReason.Name };
        await _context.CancellationReasons.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CancellationReasonAggregate cancellationReason,
        CancellationToken           cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .FirstOrDefaultAsync(e => e.Id == cancellationReason.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CancellationReasonEntity with id {cancellationReason.Id.Value} not found.");

        entity.Name = cancellationReason.Name;
        _context.CancellationReasons.Update(entity);
    }

    public async Task DeleteAsync(
        CancellationReasonId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CancellationReasonEntity with id {id.Value} not found.");

        _context.CancellationReasons.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/CancellationReason/UI/CancellationReasonConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.Interfaces;

public sealed class CancellationReasonConsoleUI
{
    private readonly ICancellationReasonService _service;

    public CancellationReasonConsoleUI(ICancellationReasonService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CANCELLATION REASON MODULE ==========");
            Console.WriteLine("1. List all cancellation reasons");
            Console.WriteLine("2. Get cancellation reason by ID");
            Console.WriteLine("3. Create cancellation reason");
            Console.WriteLine("4. Update cancellation reason");
            Console.WriteLine("5. Delete cancellation reason");
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
        Console.WriteLine("\n--- All Cancellation Reasons ---");

        foreach (var r in reasons)
            Console.WriteLine($"  [{r.Id}] {r.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter cancellation reason ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var reason = await _service.GetByIdAsync(id);

        if (reason is null)
            Console.WriteLine($"Cancellation reason with ID {id} not found.");
        else
            Console.WriteLine($"  [{reason.Id}] {reason.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Name (e.g. WEATHER, TECHNICAL, COMMERCIAL, FORCE_MAJEURE): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Cancellation reason created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter cancellation reason ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Cancellation reason updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter cancellation reason ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Cancellation reason deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── CancellationReason Module ─────────────────────────────────────────────────
builder.Services.AddScoped<ICancellationReasonRepository, CancellationReasonRepository>();
builder.Services.AddScoped<CreateCancellationReasonUseCase>();
builder.Services.AddScoped<DeleteCancellationReasonUseCase>();
builder.Services.AddScoped<GetAllCancellationReasonsUseCase>();
builder.Services.AddScoped<GetCancellationReasonByIdUseCase>();
builder.Services.AddScoped<UpdateCancellationReasonUseCase>();
builder.Services.AddScoped<ICancellationReasonService, CancellationReasonService>();
builder.Services.AddScoped<CancellationReasonConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/CancellationReason/
├── Application/
│   ├── Interfaces/
│   │   └── ICancellationReasonService.cs
│   ├── Services/
│   │   └── CancellationReasonService.cs
│   └── UseCases/
│       ├── CreateCancellationReasonUseCase.cs
│       ├── DeleteCancellationReasonUseCase.cs
│       ├── GetAllCancellationReasonsUseCase.cs
│       ├── GetCancellationReasonByIdUseCase.cs
│       └── UpdateCancellationReasonUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── CancellationReasonAggregate.cs
│   ├── Repositories/
│   │   └── ICancellationReasonRepository.cs
│   └── valueObject/
│       └── CancellationReasonId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── CancellationReasonEntity.cs
│   │   └── CancellationReasonEntityConfiguration.cs
│   └── repository/
│       └── CancellationReasonRepository.cs
└── UI/
    └── CancellationReasonConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo CancellationReason_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
