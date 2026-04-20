# Módulo: CheckInStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus`  
**Raíz de archivos:** `src/Modules/CheckInStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `check_in_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: PENDING, CHECKED_IN, BOARDED, NO_SHOW |

Tabla catálogo mínima. Sin `created_at`, `updated_at` ni FKs en el DDL.  
Nombre normalizado a `ToUpperInvariant()` para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/CheckInStatus/Domain/valueObject/CheckInStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public sealed class CheckInStatusId
{
    public int Value { get; }

    public CheckInStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CheckInStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CheckInStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/CheckInStatus/Domain/aggregate/CheckInStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados del proceso de check-in.
/// Valores esperados: PENDING, CHECKED_IN, BOARDED, NO_SHOW.
/// Nombre normalizado a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class CheckInStatusAggregate
{
    public CheckInStatusId Id   { get; private set; }
    public string          Name { get; private set; }

    private CheckInStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public CheckInStatusAggregate(CheckInStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CheckInStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "CheckInStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("CheckInStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException(
                "CheckInStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/CheckInStatus/Domain/Repositories/ICheckInStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public interface ICheckInStatusRepository
{
    Task<CheckInStatusAggregate?>             GetByIdAsync(CheckInStatusId id,                  CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInStatusAggregate>> GetAllAsync(                                       CancellationToken cancellationToken = default);
    Task                                      AddAsync(CheckInStatusAggregate checkInStatus,     CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(CheckInStatusAggregate checkInStatus,  CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(CheckInStatusId id,                    CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/CheckInStatus/Application/Interfaces/ICheckInStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;

public interface ICheckInStatusService
{
    Task<CheckInStatusDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInStatusDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<CheckInStatusDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                                UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record CheckInStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/CheckInStatus/Application/UseCases/CreateCheckInStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreateCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // CheckInStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var checkInStatus = new CheckInStatusAggregate(new CheckInStatusId(1), name);

        await _repository.AddAsync(checkInStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return checkInStatus;
    }
}
```

---

### RUTA: `src/Modules/CheckInStatus/Application/UseCases/DeleteCheckInStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeleteCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CheckInStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CheckInStatus/Application/UseCases/GetAllCheckInStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;

public sealed class GetAllCheckInStatusesUseCase
{
    private readonly ICheckInStatusRepository _repository;

    public GetAllCheckInStatusesUseCase(ICheckInStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CheckInStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/CheckInStatus/Application/UseCases/GetCheckInStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public sealed class GetCheckInStatusByIdUseCase
{
    private readonly ICheckInStatusRepository _repository;

    public GetCheckInStatusByIdUseCase(ICheckInStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CheckInStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/CheckInStatus/Application/UseCases/UpdateCheckInStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdateCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var checkInStatus = await _repository.GetByIdAsync(new CheckInStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CheckInStatus with id {id} was not found.");

        checkInStatus.UpdateName(newName);
        await _repository.UpdateAsync(checkInStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CheckInStatus/Application/Services/CheckInStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

public sealed class CheckInStatusService : ICheckInStatusService
{
    private readonly CreateCheckInStatusUseCase   _create;
    private readonly DeleteCheckInStatusUseCase   _delete;
    private readonly GetAllCheckInStatusesUseCase _getAll;
    private readonly GetCheckInStatusByIdUseCase  _getById;
    private readonly UpdateCheckInStatusUseCase   _update;

    public CheckInStatusService(
        CreateCheckInStatusUseCase  create,
        DeleteCheckInStatusUseCase  delete,
        GetAllCheckInStatusesUseCase getAll,
        GetCheckInStatusByIdUseCase getById,
        UpdateCheckInStatusUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CheckInStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new CheckInStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CheckInStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new CheckInStatusDto(a.Id.Value, a.Name));
    }

    public async Task<CheckInStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new CheckInStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/CheckInStatus/Infrastructure/entity/CheckInStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Entity;

public sealed class CheckInStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/CheckInStatus/Infrastructure/entity/CheckInStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CheckInStatusEntityConfiguration : IEntityTypeConfiguration<CheckInStatusEntity>
{
    public void Configure(EntityTypeBuilder<CheckInStatusEntity> builder)
    {
        builder.ToTable("check_in_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("check_in_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_check_in_status_name");
    }
}
```

---

### RUTA: `src/Modules/CheckInStatus/Infrastructure/repository/CheckInStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CheckInStatusRepository : ICheckInStatusRepository
{
    private readonly AppDbContext _context;

    public CheckInStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    private static CheckInStatusAggregate ToDomain(CheckInStatusEntity entity)
        => new(new CheckInStatusId(entity.Id), entity.Name);

    public async Task<CheckInStatusAggregate?> GetByIdAsync(
        CheckInStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CheckInStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.CheckInStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CheckInStatusAggregate checkInStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = new CheckInStatusEntity { Name = checkInStatus.Name };
        await _context.CheckInStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CheckInStatusAggregate checkInStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .FirstOrDefaultAsync(e => e.Id == checkInStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInStatusEntity with id {checkInStatus.Id.Value} not found.");

        entity.Name = checkInStatus.Name;
        _context.CheckInStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        CheckInStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInStatusEntity with id {id.Value} not found.");

        _context.CheckInStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/CheckInStatus/UI/CheckInStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.Interfaces;

public sealed class CheckInStatusConsoleUI
{
    private readonly ICheckInStatusService _service;

    public CheckInStatusConsoleUI(ICheckInStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CHECK-IN STATUS MODULE ==========");
            Console.WriteLine("1. List all check-in statuses");
            Console.WriteLine("2. Get check-in status by ID");
            Console.WriteLine("3. Create check-in status");
            Console.WriteLine("4. Update check-in status");
            Console.WriteLine("5. Delete check-in status");
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

    private async Task ListAllAsync()
    {
        var statuses = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Check-In Statuses ---");
        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter check-in status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var status = await _service.GetByIdAsync(id);
        if (status is null) Console.WriteLine($"Check-in status with ID {id} not found.");
        else                Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter name (e.g. PENDING, CHECKED_IN, BOARDED, NO_SHOW): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Check-in status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter check-in status ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        { Console.WriteLine("Name cannot be empty."); return; }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Check-in status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter check-in status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Check-in status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── CheckInStatus Module ──────────────────────────────────────────────────────
builder.Services.AddScoped<ICheckInStatusRepository, CheckInStatusRepository>();
builder.Services.AddScoped<CreateCheckInStatusUseCase>();
builder.Services.AddScoped<DeleteCheckInStatusUseCase>();
builder.Services.AddScoped<GetAllCheckInStatusesUseCase>();
builder.Services.AddScoped<GetCheckInStatusByIdUseCase>();
builder.Services.AddScoped<UpdateCheckInStatusUseCase>();
builder.Services.AddScoped<ICheckInStatusService, CheckInStatusService>();
builder.Services.AddScoped<CheckInStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/CheckInStatus/
├── Application/
│   ├── Interfaces/
│   │   └── ICheckInStatusService.cs
│   ├── Services/
│   │   └── CheckInStatusService.cs
│   └── UseCases/
│       ├── CreateCheckInStatusUseCase.cs
│       ├── DeleteCheckInStatusUseCase.cs
│       ├── GetAllCheckInStatusesUseCase.cs
│       ├── GetCheckInStatusByIdUseCase.cs
│       └── UpdateCheckInStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── CheckInStatusAggregate.cs
│   ├── Repositories/
│   │   └── ICheckInStatusRepository.cs
│   └── valueObject/
│       └── CheckInStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── CheckInStatusEntity.cs
│   │   └── CheckInStatusEntityConfiguration.cs
│   └── repository/
│       └── CheckInStatusRepository.cs
└── UI/
    └── CheckInStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo CheckInStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
