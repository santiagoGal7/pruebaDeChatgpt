# Módulo: RefundStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus`  
**Raíz de archivos:** `src/Modules/RefundStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `refund_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: PENDING, APPROVED, REJECTED, PROCESSED |

Tabla catálogo mínima. Sin `created_at`, `updated_at` ni FKs en el DDL.  
Nombre normalizado a `ToUpperInvariant()` para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/RefundStatus/Domain/valueObject/RefundStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;

public sealed class RefundStatusId
{
    public int Value { get; }

    public RefundStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("RefundStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is RefundStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/RefundStatus/Domain/aggregate/RefundStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados de reembolso.
/// Valores esperados: PENDING, APPROVED, REJECTED, PROCESSED.
/// Nombre normalizado a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class RefundStatusAggregate
{
    public RefundStatusId Id   { get; private set; }
    public string         Name { get; private set; }

    private RefundStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public RefundStatusAggregate(RefundStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("RefundStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "RefundStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("RefundStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException(
                "RefundStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/RefundStatus/Domain/Repositories/IRefundStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;

public interface IRefundStatusRepository
{
    Task<RefundStatusAggregate?>             GetByIdAsync(RefundStatusId id,                 CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundStatusAggregate>> GetAllAsync(                                     CancellationToken cancellationToken = default);
    Task                                     AddAsync(RefundStatusAggregate refundStatus,     CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(RefundStatusAggregate refundStatus,  CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(RefundStatusId id,                   CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/RefundStatus/Application/Interfaces/IRefundStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.Interfaces;

public interface IRefundStatusService
{
    Task<RefundStatusDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<RefundStatusDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<RefundStatusDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                               UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                               DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record RefundStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/RefundStatus/Application/UseCases/CreateRefundStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateRefundStatusUseCase
{
    private readonly IRefundStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public CreateRefundStatusUseCase(IRefundStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefundStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // RefundStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var refundStatus = new RefundStatusAggregate(new RefundStatusId(1), name);

        await _repository.AddAsync(refundStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return refundStatus;
    }
}
```

---

### RUTA: `src/Modules/RefundStatus/Application/UseCases/DeleteRefundStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteRefundStatusUseCase
{
    private readonly IRefundStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public DeleteRefundStatusUseCase(IRefundStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new RefundStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/RefundStatus/Application/UseCases/GetAllRefundStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;

public sealed class GetAllRefundStatusesUseCase
{
    private readonly IRefundStatusRepository _repository;

    public GetAllRefundStatusesUseCase(IRefundStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RefundStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/RefundStatus/Application/UseCases/GetRefundStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;

public sealed class GetRefundStatusByIdUseCase
{
    private readonly IRefundStatusRepository _repository;

    public GetRefundStatusByIdUseCase(IRefundStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<RefundStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new RefundStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/RefundStatus/Application/UseCases/UpdateRefundStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateRefundStatusUseCase
{
    private readonly IRefundStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public UpdateRefundStatusUseCase(IRefundStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var refundStatus = await _repository.GetByIdAsync(new RefundStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"RefundStatus with id {id} was not found.");

        refundStatus.UpdateName(newName);
        await _repository.UpdateAsync(refundStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/RefundStatus/Application/Services/RefundStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.UseCases;

public sealed class RefundStatusService : IRefundStatusService
{
    private readonly CreateRefundStatusUseCase   _create;
    private readonly DeleteRefundStatusUseCase   _delete;
    private readonly GetAllRefundStatusesUseCase _getAll;
    private readonly GetRefundStatusByIdUseCase  _getById;
    private readonly UpdateRefundStatusUseCase   _update;

    public RefundStatusService(
        CreateRefundStatusUseCase  create,
        DeleteRefundStatusUseCase  delete,
        GetAllRefundStatusesUseCase getAll,
        GetRefundStatusByIdUseCase getById,
        UpdateRefundStatusUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<RefundStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new RefundStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<RefundStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new RefundStatusDto(a.Id.Value, a.Name));
    }

    public async Task<RefundStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new RefundStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/RefundStatus/Infrastructure/entity/RefundStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Infrastructure.Entity;

public sealed class RefundStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/RefundStatus/Infrastructure/entity/RefundStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RefundStatusEntityConfiguration : IEntityTypeConfiguration<RefundStatusEntity>
{
    public void Configure(EntityTypeBuilder<RefundStatusEntity> builder)
    {
        builder.ToTable("refund_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("refund_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_refund_status_name");
    }
}
```

---

### RUTA: `src/Modules/RefundStatus/Infrastructure/repository/RefundStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class RefundStatusRepository : IRefundStatusRepository
{
    private readonly AppDbContext _context;

    public RefundStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static RefundStatusAggregate ToDomain(RefundStatusEntity entity)
        => new(new RefundStatusId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<RefundStatusAggregate?> GetByIdAsync(
        RefundStatusId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefundStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<RefundStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.RefundStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        RefundStatusAggregate refundStatus,
        CancellationToken     cancellationToken = default)
    {
        var entity = new RefundStatusEntity { Name = refundStatus.Name };
        await _context.RefundStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        RefundStatusAggregate refundStatus,
        CancellationToken     cancellationToken = default)
    {
        var entity = await _context.RefundStatuses
            .FirstOrDefaultAsync(e => e.Id == refundStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RefundStatusEntity with id {refundStatus.Id.Value} not found.");

        entity.Name = refundStatus.Name;
        _context.RefundStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        RefundStatusId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefundStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RefundStatusEntity with id {id.Value} not found.");

        _context.RefundStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/RefundStatus/UI/RefundStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.RefundStatus.Application.Interfaces;

public sealed class RefundStatusConsoleUI
{
    private readonly IRefundStatusService _service;

    public RefundStatusConsoleUI(IRefundStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== REFUND STATUS MODULE ==========");
            Console.WriteLine("1. List all refund statuses");
            Console.WriteLine("2. Get refund status by ID");
            Console.WriteLine("3. Create refund status");
            Console.WriteLine("4. Update refund status");
            Console.WriteLine("5. Delete refund status");
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
        var statuses = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Refund Statuses ---");

        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter refund status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var status = await _service.GetByIdAsync(id);

        if (status is null)
            Console.WriteLine($"Refund status with ID {id} not found.");
        else
            Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter refund status name (e.g. PENDING, APPROVED, REJECTED, PROCESSED): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Refund status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter refund status ID to update: ");
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
        Console.WriteLine("Refund status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter refund status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Refund status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── RefundStatus Module ───────────────────────────────────────────────────────
builder.Services.AddScoped<IRefundStatusRepository, RefundStatusRepository>();
builder.Services.AddScoped<CreateRefundStatusUseCase>();
builder.Services.AddScoped<DeleteRefundStatusUseCase>();
builder.Services.AddScoped<GetAllRefundStatusesUseCase>();
builder.Services.AddScoped<GetRefundStatusByIdUseCase>();
builder.Services.AddScoped<UpdateRefundStatusUseCase>();
builder.Services.AddScoped<IRefundStatusService, RefundStatusService>();
builder.Services.AddScoped<RefundStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/RefundStatus/
├── Application/
│   ├── Interfaces/
│   │   └── IRefundStatusService.cs
│   ├── Services/
│   │   └── RefundStatusService.cs
│   └── UseCases/
│       ├── CreateRefundStatusUseCase.cs
│       ├── DeleteRefundStatusUseCase.cs
│       ├── GetAllRefundStatusesUseCase.cs
│       ├── GetRefundStatusByIdUseCase.cs
│       └── UpdateRefundStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── RefundStatusAggregate.cs
│   ├── Repositories/
│   │   └── IRefundStatusRepository.cs
│   └── valueObject/
│       └── RefundStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── RefundStatusEntity.cs
│   │   └── RefundStatusEntityConfiguration.cs
│   └── repository/
│       └── RefundStatusRepository.cs
└── UI/
    └── RefundStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo RefundStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
