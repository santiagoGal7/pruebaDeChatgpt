# Módulo: ReservationStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus`  
**Raíz de archivos:** `src/Modules/ReservationStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `reservation_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: PENDING, CONFIRMED, CANCELLED |

Tabla catálogo simple. Sin `created_at`, `updated_at` ni FKs en el DDL.  
El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/ReservationStatus/Domain/valueObject/ReservationStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;

public sealed class ReservationStatusId
{
    public int Value { get; }

    public ReservationStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("ReservationStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is ReservationStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/ReservationStatus/Domain/aggregate/ReservationStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados del ciclo de vida de una reserva.
/// Valores esperados en producción: PENDING, CONFIRMED, CANCELLED.
/// El nombre se normaliza a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class ReservationStatusAggregate
{
    public ReservationStatusId Id   { get; private set; }
    public string              Name { get; private set; }

    private ReservationStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public ReservationStatusAggregate(ReservationStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("ReservationStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException("ReservationStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("ReservationStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException("ReservationStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/ReservationStatus/Domain/Repositories/IReservationStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;

public interface IReservationStatusRepository
{
    Task<ReservationStatusAggregate?>             GetByIdAsync(ReservationStatusId id,                    CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationStatusAggregate>> GetAllAsync(                                             CancellationToken cancellationToken = default);
    Task                                          AddAsync(ReservationStatusAggregate reservationStatus,   CancellationToken cancellationToken = default);
    Task                                          UpdateAsync(ReservationStatusAggregate reservationStatus,CancellationToken cancellationToken = default);
    Task                                          DeleteAsync(ReservationStatusId id,                      CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/ReservationStatus/Application/Interfaces/IReservationStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.Interfaces;

public interface IReservationStatusService
{
    Task<ReservationStatusDto?>             GetByIdAsync(int id,           CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationStatusDto>> GetAllAsync(                   CancellationToken cancellationToken = default);
    Task<ReservationStatusDto>              CreateAsync(string name,       CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(int id,            CancellationToken cancellationToken = default);
}

public sealed record ReservationStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/ReservationStatus/Application/UseCases/CreateReservationStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateReservationStatusUseCase
{
    private readonly IReservationStatusRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public CreateReservationStatusUseCase(IReservationStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // ReservationStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var reservationStatus = new ReservationStatusAggregate(new ReservationStatusId(1), name);

        await _repository.AddAsync(reservationStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return reservationStatus;
    }
}
```

---

### RUTA: `src/Modules/ReservationStatus/Application/UseCases/DeleteReservationStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteReservationStatusUseCase
{
    private readonly IReservationStatusRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public DeleteReservationStatusUseCase(IReservationStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new ReservationStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ReservationStatus/Application/UseCases/GetAllReservationStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;

public sealed class GetAllReservationStatusesUseCase
{
    private readonly IReservationStatusRepository _repository;

    public GetAllReservationStatusesUseCase(IReservationStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ReservationStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/ReservationStatus/Application/UseCases/GetReservationStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;

public sealed class GetReservationStatusByIdUseCase
{
    private readonly IReservationStatusRepository _repository;

    public GetReservationStatusByIdUseCase(IReservationStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservationStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new ReservationStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/ReservationStatus/Application/UseCases/UpdateReservationStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateReservationStatusUseCase
{
    private readonly IReservationStatusRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public UpdateReservationStatusUseCase(IReservationStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var reservationStatus = await _repository.GetByIdAsync(new ReservationStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"ReservationStatus with id {id} was not found.");

        reservationStatus.UpdateName(newName);
        await _repository.UpdateAsync(reservationStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/ReservationStatus/Application/Services/ReservationStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.UseCases;

public sealed class ReservationStatusService : IReservationStatusService
{
    private readonly CreateReservationStatusUseCase   _create;
    private readonly DeleteReservationStatusUseCase   _delete;
    private readonly GetAllReservationStatusesUseCase _getAll;
    private readonly GetReservationStatusByIdUseCase  _getById;
    private readonly UpdateReservationStatusUseCase   _update;

    public ReservationStatusService(
        CreateReservationStatusUseCase   create,
        DeleteReservationStatusUseCase   delete,
        GetAllReservationStatusesUseCase getAll,
        GetReservationStatusByIdUseCase  getById,
        UpdateReservationStatusUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<ReservationStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new ReservationStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<ReservationStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new ReservationStatusDto(a.Id.Value, a.Name));
    }

    public async Task<ReservationStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new ReservationStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/ReservationStatus/Infrastructure/entity/ReservationStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Infrastructure.Entity;

public sealed class ReservationStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/ReservationStatus/Infrastructure/entity/ReservationStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ReservationStatusEntityConfiguration : IEntityTypeConfiguration<ReservationStatusEntity>
{
    public void Configure(EntityTypeBuilder<ReservationStatusEntity> builder)
    {
        builder.ToTable("reservation_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("reservation_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_reservation_status_name");
    }
}
```

---

### RUTA: `src/Modules/ReservationStatus/Infrastructure/repository/ReservationStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class ReservationStatusRepository : IReservationStatusRepository
{
    private readonly AppDbContext _context;

    public ReservationStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static ReservationStatusAggregate ToDomain(ReservationStatusEntity entity)
        => new(new ReservationStatusId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<ReservationStatusAggregate?> GetByIdAsync(
        ReservationStatusId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.ReservationStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<ReservationStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.ReservationStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        ReservationStatusAggregate reservationStatus,
        CancellationToken          cancellationToken = default)
    {
        var entity = new ReservationStatusEntity { Name = reservationStatus.Name };
        await _context.ReservationStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        ReservationStatusAggregate reservationStatus,
        CancellationToken          cancellationToken = default)
    {
        var entity = await _context.ReservationStatuses
            .FirstOrDefaultAsync(e => e.Id == reservationStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationStatusEntity with id {reservationStatus.Id.Value} not found.");

        entity.Name = reservationStatus.Name;
        _context.ReservationStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        ReservationStatusId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.ReservationStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"ReservationStatusEntity with id {id.Value} not found.");

        _context.ReservationStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/ReservationStatus/UI/ReservationStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.ReservationStatus.Application.Interfaces;

public sealed class ReservationStatusConsoleUI
{
    private readonly IReservationStatusService _service;

    public ReservationStatusConsoleUI(IReservationStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== RESERVATION STATUS MODULE ==========");
            Console.WriteLine("1. List all reservation statuses");
            Console.WriteLine("2. Get reservation status by ID");
            Console.WriteLine("3. Create reservation status");
            Console.WriteLine("4. Update reservation status");
            Console.WriteLine("5. Delete reservation status");
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
        Console.WriteLine("\n--- All Reservation Statuses ---");

        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter reservation status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var status = await _service.GetByIdAsync(id);

        if (status is null)
            Console.WriteLine($"Reservation status with ID {id} not found.");
        else
            Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter reservation status name (e.g. PENDING, CONFIRMED, CANCELLED): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Reservation status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter reservation status ID to update: ");
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
        Console.WriteLine("Reservation status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter reservation status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Reservation status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── ReservationStatus Module ──────────────────────────────────────────────────
builder.Services.AddScoped<IReservationStatusRepository, ReservationStatusRepository>();
builder.Services.AddScoped<CreateReservationStatusUseCase>();
builder.Services.AddScoped<DeleteReservationStatusUseCase>();
builder.Services.AddScoped<GetAllReservationStatusesUseCase>();
builder.Services.AddScoped<GetReservationStatusByIdUseCase>();
builder.Services.AddScoped<UpdateReservationStatusUseCase>();
builder.Services.AddScoped<IReservationStatusService, ReservationStatusService>();
builder.Services.AddScoped<ReservationStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/ReservationStatus/
├── Application/
│   ├── Interfaces/
│   │   └── IReservationStatusService.cs
│   ├── Services/
│   │   └── ReservationStatusService.cs
│   └── UseCases/
│       ├── CreateReservationStatusUseCase.cs
│       ├── DeleteReservationStatusUseCase.cs
│       ├── GetAllReservationStatusesUseCase.cs
│       ├── GetReservationStatusByIdUseCase.cs
│       └── UpdateReservationStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── ReservationStatusAggregate.cs
│   ├── Repositories/
│   │   └── IReservationStatusRepository.cs
│   └── valueObject/
│       └── ReservationStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── ReservationStatusEntity.cs
│   │   └── ReservationStatusEntityConfiguration.cs
│   └── repository/
│       └── ReservationStatusRepository.cs
└── UI/
    └── ReservationStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo ReservationStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
