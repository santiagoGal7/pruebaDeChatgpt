# Módulo: TicketStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus`  
**Raíz de archivos:** `src/Modules/TicketStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `ticket_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: ISSUED, USED, CANCELLED, REFUNDED |

Tabla catálogo mínima. Sin `created_at`, `updated_at` ni FKs en el DDL.  
Nombre normalizado a `ToUpperInvariant()` para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/TicketStatus/Domain/valueObject/TicketStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;

public sealed class TicketStatusId
{
    public int Value { get; }

    public TicketStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("TicketStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is TicketStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/TicketStatus/Domain/aggregate/TicketStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados de tiquete.
/// Valores esperados: ISSUED, USED, CANCELLED, REFUNDED.
/// Nombre normalizado a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class TicketStatusAggregate
{
    public TicketStatusId Id   { get; private set; }
    public string         Name { get; private set; }

    private TicketStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public TicketStatusAggregate(TicketStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("TicketStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "TicketStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("TicketStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException(
                "TicketStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/TicketStatus/Domain/Repositories/ITicketStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;

public interface ITicketStatusRepository
{
    Task<TicketStatusAggregate?>             GetByIdAsync(TicketStatusId id,                CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketStatusAggregate>> GetAllAsync(                                    CancellationToken cancellationToken = default);
    Task                                     AddAsync(TicketStatusAggregate ticketStatus,   CancellationToken cancellationToken = default);
    Task                                     UpdateAsync(TicketStatusAggregate ticketStatus,CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(TicketStatusId id,                  CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/TicketStatus/Application/Interfaces/ITicketStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.Interfaces;

public interface ITicketStatusService
{
    Task<TicketStatusDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketStatusDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<TicketStatusDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                               UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                               DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record TicketStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/TicketStatus/Application/UseCases/CreateTicketStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateTicketStatusUseCase
{
    private readonly ITicketStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public CreateTicketStatusUseCase(ITicketStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // TicketStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var ticketStatus = new TicketStatusAggregate(new TicketStatusId(1), name);

        await _repository.AddAsync(ticketStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return ticketStatus;
    }
}
```

---

### RUTA: `src/Modules/TicketStatus/Application/UseCases/DeleteTicketStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteTicketStatusUseCase
{
    private readonly ITicketStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public DeleteTicketStatusUseCase(ITicketStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new TicketStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/TicketStatus/Application/UseCases/GetAllTicketStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;

public sealed class GetAllTicketStatusesUseCase
{
    private readonly ITicketStatusRepository _repository;

    public GetAllTicketStatusesUseCase(ITicketStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TicketStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/TicketStatus/Application/UseCases/GetTicketStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;

public sealed class GetTicketStatusByIdUseCase
{
    private readonly ITicketStatusRepository _repository;

    public GetTicketStatusByIdUseCase(ITicketStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<TicketStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new TicketStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/TicketStatus/Application/UseCases/UpdateTicketStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateTicketStatusUseCase
{
    private readonly ITicketStatusRepository _repository;
    private readonly IUnitOfWork             _unitOfWork;

    public UpdateTicketStatusUseCase(ITicketStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var ticketStatus = await _repository.GetByIdAsync(new TicketStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"TicketStatus with id {id} was not found.");

        ticketStatus.UpdateName(newName);
        await _repository.UpdateAsync(ticketStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/TicketStatus/Application/Services/TicketStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.UseCases;

public sealed class TicketStatusService : ITicketStatusService
{
    private readonly CreateTicketStatusUseCase   _create;
    private readonly DeleteTicketStatusUseCase   _delete;
    private readonly GetAllTicketStatusesUseCase _getAll;
    private readonly GetTicketStatusByIdUseCase  _getById;
    private readonly UpdateTicketStatusUseCase   _update;

    public TicketStatusService(
        CreateTicketStatusUseCase  create,
        DeleteTicketStatusUseCase  delete,
        GetAllTicketStatusesUseCase getAll,
        GetTicketStatusByIdUseCase getById,
        UpdateTicketStatusUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<TicketStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new TicketStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<TicketStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new TicketStatusDto(a.Id.Value, a.Name));
    }

    public async Task<TicketStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new TicketStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/TicketStatus/Infrastructure/entity/TicketStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Infrastructure.Entity;

public sealed class TicketStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/TicketStatus/Infrastructure/entity/TicketStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TicketStatusEntityConfiguration : IEntityTypeConfiguration<TicketStatusEntity>
{
    public void Configure(EntityTypeBuilder<TicketStatusEntity> builder)
    {
        builder.ToTable("ticket_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("ticket_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_ticket_status_name");
    }
}
```

---

### RUTA: `src/Modules/TicketStatus/Infrastructure/repository/TicketStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class TicketStatusRepository : ITicketStatusRepository
{
    private readonly AppDbContext _context;

    public TicketStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    private static TicketStatusAggregate ToDomain(TicketStatusEntity entity)
        => new(new TicketStatusId(entity.Id), entity.Name);

    public async Task<TicketStatusAggregate?> GetByIdAsync(
        TicketStatusId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TicketStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<TicketStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TicketStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        TicketStatusAggregate ticketStatus,
        CancellationToken     cancellationToken = default)
    {
        var entity = new TicketStatusEntity { Name = ticketStatus.Name };
        await _context.TicketStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        TicketStatusAggregate ticketStatus,
        CancellationToken     cancellationToken = default)
    {
        var entity = await _context.TicketStatuses
            .FirstOrDefaultAsync(e => e.Id == ticketStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketStatusEntity with id {ticketStatus.Id.Value} not found.");

        entity.Name = ticketStatus.Name;
        _context.TicketStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        TicketStatusId    id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TicketStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketStatusEntity with id {id.Value} not found.");

        _context.TicketStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/TicketStatus/UI/TicketStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketStatus.Application.Interfaces;

public sealed class TicketStatusConsoleUI
{
    private readonly ITicketStatusService _service;

    public TicketStatusConsoleUI(ITicketStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== TICKET STATUS MODULE ==========");
            Console.WriteLine("1. List all ticket statuses");
            Console.WriteLine("2. Get ticket status by ID");
            Console.WriteLine("3. Create ticket status");
            Console.WriteLine("4. Update ticket status");
            Console.WriteLine("5. Delete ticket status");
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
        Console.WriteLine("\n--- All Ticket Statuses ---");
        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter ticket status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var status = await _service.GetByIdAsync(id);
        if (status is null) Console.WriteLine($"Ticket status with ID {id} not found.");
        else                Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter name (e.g. ISSUED, USED, CANCELLED, REFUNDED): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Ticket status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter ticket status ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Enter new name: ");
        var newName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        { Console.WriteLine("Name cannot be empty."); return; }

        await _service.UpdateAsync(id, newName);
        Console.WriteLine("Ticket status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter ticket status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Ticket status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── TicketStatus Module ───────────────────────────────────────────────────────
builder.Services.AddScoped<ITicketStatusRepository, TicketStatusRepository>();
builder.Services.AddScoped<CreateTicketStatusUseCase>();
builder.Services.AddScoped<DeleteTicketStatusUseCase>();
builder.Services.AddScoped<GetAllTicketStatusesUseCase>();
builder.Services.AddScoped<GetTicketStatusByIdUseCase>();
builder.Services.AddScoped<UpdateTicketStatusUseCase>();
builder.Services.AddScoped<ITicketStatusService, TicketStatusService>();
builder.Services.AddScoped<TicketStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/TicketStatus/
├── Application/
│   ├── Interfaces/
│   │   └── ITicketStatusService.cs
│   ├── Services/
│   │   └── TicketStatusService.cs
│   └── UseCases/
│       ├── CreateTicketStatusUseCase.cs
│       ├── DeleteTicketStatusUseCase.cs
│       ├── GetAllTicketStatusesUseCase.cs
│       ├── GetTicketStatusByIdUseCase.cs
│       └── UpdateTicketStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── TicketStatusAggregate.cs
│   ├── Repositories/
│   │   └── ITicketStatusRepository.cs
│   └── valueObject/
│       └── TicketStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── TicketStatusEntity.cs
│   │   └── TicketStatusEntityConfiguration.cs
│   └── repository/
│       └── TicketStatusRepository.cs
└── UI/
    └── TicketStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo TicketStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
