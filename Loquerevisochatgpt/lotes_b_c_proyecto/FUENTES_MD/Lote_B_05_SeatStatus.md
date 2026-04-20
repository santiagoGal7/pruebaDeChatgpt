# Módulo: SeatStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus`  
**Raíz de archivos:** `src/Modules/SeatStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `seat_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: AVAILABLE, OCCUPIED, BLOCKED |

Tabla catálogo simple. Sin `created_at`, `updated_at` ni FKs en el DDL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/SeatStatus/Domain/valueObject/SeatStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;

public sealed class SeatStatusId
{
    public int Value { get; }

    public SeatStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("SeatStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is SeatStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/SeatStatus/Domain/aggregate/SeatStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados de un asiento en un vuelo.
/// Valores esperados en producción: AVAILABLE, OCCUPIED, BLOCKED.
/// El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class SeatStatusAggregate
{
    public SeatStatusId Id   { get; private set; }
    public string       Name { get; private set; }

    private SeatStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public SeatStatusAggregate(SeatStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("SeatStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException("SeatStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("SeatStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException("SeatStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/SeatStatus/Domain/Repositories/ISeatStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;

public interface ISeatStatusRepository
{
    Task<SeatStatusAggregate?>             GetByIdAsync(SeatStatusId id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatStatusAggregate>> GetAllAsync(                               CancellationToken cancellationToken = default);
    Task                                   AddAsync(SeatStatusAggregate seatStatus,   CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(SeatStatusAggregate seatStatus,CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(SeatStatusId id,               CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/SeatStatus/Application/Interfaces/ISeatStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.Interfaces;

public interface ISeatStatusService
{
    Task<SeatStatusDto?>             GetByIdAsync(int id,          CancellationToken cancellationToken = default);
    Task<IEnumerable<SeatStatusDto>> GetAllAsync(                  CancellationToken cancellationToken = default);
    Task<SeatStatusDto>              CreateAsync(string name,      CancellationToken cancellationToken = default);
    Task                             UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
    Task                             DeleteAsync(int id,           CancellationToken cancellationToken = default);
}

public sealed record SeatStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/SeatStatus/Application/UseCases/CreateSeatStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateSeatStatusUseCase
{
    private readonly ISeatStatusRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public CreateSeatStatusUseCase(ISeatStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SeatStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // SeatStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var seatStatus = new SeatStatusAggregate(new SeatStatusId(1), name);

        await _repository.AddAsync(seatStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return seatStatus;
    }
}
```

---

### RUTA: `src/Modules/SeatStatus/Application/UseCases/DeleteSeatStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteSeatStatusUseCase
{
    private readonly ISeatStatusRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public DeleteSeatStatusUseCase(ISeatStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new SeatStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/SeatStatus/Application/UseCases/GetAllSeatStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;

public sealed class GetAllSeatStatusesUseCase
{
    private readonly ISeatStatusRepository _repository;

    public GetAllSeatStatusesUseCase(ISeatStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SeatStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/SeatStatus/Application/UseCases/GetSeatStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;

public sealed class GetSeatStatusByIdUseCase
{
    private readonly ISeatStatusRepository _repository;

    public GetSeatStatusByIdUseCase(ISeatStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<SeatStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new SeatStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/SeatStatus/Application/UseCases/UpdateSeatStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateSeatStatusUseCase
{
    private readonly ISeatStatusRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public UpdateSeatStatusUseCase(ISeatStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var seatStatus = await _repository.GetByIdAsync(new SeatStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"SeatStatus with id {id} was not found.");

        seatStatus.UpdateName(newName);
        await _repository.UpdateAsync(seatStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/SeatStatus/Application/Services/SeatStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.UseCases;

public sealed class SeatStatusService : ISeatStatusService
{
    private readonly CreateSeatStatusUseCase   _create;
    private readonly DeleteSeatStatusUseCase   _delete;
    private readonly GetAllSeatStatusesUseCase _getAll;
    private readonly GetSeatStatusByIdUseCase  _getById;
    private readonly UpdateSeatStatusUseCase   _update;

    public SeatStatusService(
        CreateSeatStatusUseCase   create,
        DeleteSeatStatusUseCase   delete,
        GetAllSeatStatusesUseCase getAll,
        GetSeatStatusByIdUseCase  getById,
        UpdateSeatStatusUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<SeatStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new SeatStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<SeatStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new SeatStatusDto(a.Id.Value, a.Name));
    }

    public async Task<SeatStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new SeatStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/SeatStatus/Infrastructure/entity/SeatStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Infrastructure.Entity;

public sealed class SeatStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/SeatStatus/Infrastructure/entity/SeatStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class SeatStatusEntityConfiguration : IEntityTypeConfiguration<SeatStatusEntity>
{
    public void Configure(EntityTypeBuilder<SeatStatusEntity> builder)
    {
        builder.ToTable("seat_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("seat_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_seat_status_name");
    }
}
```

---

### RUTA: `src/Modules/SeatStatus/Infrastructure/repository/SeatStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class SeatStatusRepository : ISeatStatusRepository
{
    private readonly AppDbContext _context;

    public SeatStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static SeatStatusAggregate ToDomain(SeatStatusEntity entity)
        => new(new SeatStatusId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<SeatStatusAggregate?> GetByIdAsync(
        SeatStatusId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SeatStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<SeatStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.SeatStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        SeatStatusAggregate seatStatus,
        CancellationToken   cancellationToken = default)
    {
        var entity = new SeatStatusEntity { Name = seatStatus.Name };
        await _context.SeatStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        SeatStatusAggregate seatStatus,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.SeatStatuses
            .FirstOrDefaultAsync(e => e.Id == seatStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SeatStatusEntity with id {seatStatus.Id.Value} not found.");

        entity.Name = seatStatus.Name;
        _context.SeatStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        SeatStatusId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.SeatStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"SeatStatusEntity with id {id.Value} not found.");

        _context.SeatStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/SeatStatus/UI/SeatStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.SeatStatus.Application.Interfaces;

public sealed class SeatStatusConsoleUI
{
    private readonly ISeatStatusService _service;

    public SeatStatusConsoleUI(ISeatStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== SEAT STATUS MODULE ==========");
            Console.WriteLine("1. List all seat statuses");
            Console.WriteLine("2. Get seat status by ID");
            Console.WriteLine("3. Create seat status");
            Console.WriteLine("4. Update seat status");
            Console.WriteLine("5. Delete seat status");
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
        Console.WriteLine("\n--- All Seat Statuses ---");

        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter seat status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var status = await _service.GetByIdAsync(id);

        if (status is null)
            Console.WriteLine($"Seat status with ID {id} not found.");
        else
            Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter seat status name (e.g. AVAILABLE, OCCUPIED, BLOCKED): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Seat status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter seat status ID to update: ");
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
        Console.WriteLine("Seat status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter seat status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Seat status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── SeatStatus Module ─────────────────────────────────────────────────────────
builder.Services.AddScoped<ISeatStatusRepository, SeatStatusRepository>();
builder.Services.AddScoped<CreateSeatStatusUseCase>();
builder.Services.AddScoped<DeleteSeatStatusUseCase>();
builder.Services.AddScoped<GetAllSeatStatusesUseCase>();
builder.Services.AddScoped<GetSeatStatusByIdUseCase>();
builder.Services.AddScoped<UpdateSeatStatusUseCase>();
builder.Services.AddScoped<ISeatStatusService, SeatStatusService>();
builder.Services.AddScoped<SeatStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/SeatStatus/
├── Application/
│   ├── Interfaces/
│   │   └── ISeatStatusService.cs
│   ├── Services/
│   │   └── SeatStatusService.cs
│   └── UseCases/
│       ├── CreateSeatStatusUseCase.cs
│       ├── DeleteSeatStatusUseCase.cs
│       ├── GetAllSeatStatusesUseCase.cs
│       ├── GetSeatStatusByIdUseCase.cs
│       └── UpdateSeatStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── SeatStatusAggregate.cs
│   ├── Repositories/
│   │   └── ISeatStatusRepository.cs
│   └── valueObject/
│       └── SeatStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── SeatStatusEntity.cs
│   │   └── SeatStatusEntityConfiguration.cs
│   └── repository/
│       └── SeatStatusRepository.cs
└── UI/
    └── SeatStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo SeatStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
