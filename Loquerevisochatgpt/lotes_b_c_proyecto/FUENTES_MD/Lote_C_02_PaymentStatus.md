# Módulo: PaymentStatus
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus`  
**Raíz de archivos:** `src/Modules/PaymentStatus/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `payment_status_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: PENDING, PAID, REJECTED |

Tabla catálogo mínima. Sin `created_at`, `updated_at` ni FKs en el DDL.  
Nombre normalizado a `ToUpperInvariant()` para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/PaymentStatus/Domain/valueObject/PaymentStatusId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;

public sealed class PaymentStatusId
{
    public int Value { get; }

    public PaymentStatusId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("PaymentStatusId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is PaymentStatusId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/PaymentStatus/Domain/aggregate/PaymentStatusAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;

/// <summary>
/// Catálogo de estados de pago.
/// Valores esperados: PENDING, PAID, REJECTED.
/// Nombre normalizado a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class PaymentStatusAggregate
{
    public PaymentStatusId Id   { get; private set; }
    public string          Name { get; private set; }

    private PaymentStatusAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public PaymentStatusAggregate(PaymentStatusId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("PaymentStatus name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "PaymentStatus name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("PaymentStatus name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException(
                "PaymentStatus name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/PaymentStatus/Domain/Repositories/IPaymentStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;

public interface IPaymentStatusRepository
{
    Task<PaymentStatusAggregate?>             GetByIdAsync(PaymentStatusId id,                CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentStatusAggregate>> GetAllAsync(                                     CancellationToken cancellationToken = default);
    Task                                      AddAsync(PaymentStatusAggregate paymentStatus,   CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(PaymentStatusAggregate paymentStatus,CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(PaymentStatusId id,                  CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/PaymentStatus/Application/Interfaces/IPaymentStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.Interfaces;

public interface IPaymentStatusService
{
    Task<PaymentStatusDto?>             GetByIdAsync(int id,           CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentStatusDto>> GetAllAsync(                   CancellationToken cancellationToken = default);
    Task<PaymentStatusDto>              CreateAsync(string name,       CancellationToken cancellationToken = default);
    Task                                UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,            CancellationToken cancellationToken = default);
}

public sealed record PaymentStatusDto(int Id, string Name);
```

---

### RUTA: `src/Modules/PaymentStatus/Application/UseCases/CreatePaymentStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreatePaymentStatusUseCase
{
    private readonly IPaymentStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreatePaymentStatusUseCase(IPaymentStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // PaymentStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var paymentStatus = new PaymentStatusAggregate(new PaymentStatusId(1), name);

        await _repository.AddAsync(paymentStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return paymentStatus;
    }
}
```

---

### RUTA: `src/Modules/PaymentStatus/Application/UseCases/DeletePaymentStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeletePaymentStatusUseCase
{
    private readonly IPaymentStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeletePaymentStatusUseCase(IPaymentStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new PaymentStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PaymentStatus/Application/UseCases/GetAllPaymentStatusesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;

public sealed class GetAllPaymentStatusesUseCase
{
    private readonly IPaymentStatusRepository _repository;

    public GetAllPaymentStatusesUseCase(IPaymentStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/PaymentStatus/Application/UseCases/GetPaymentStatusByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;

public sealed class GetPaymentStatusByIdUseCase
{
    private readonly IPaymentStatusRepository _repository;

    public GetPaymentStatusByIdUseCase(IPaymentStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new PaymentStatusId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/PaymentStatus/Application/UseCases/UpdatePaymentStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdatePaymentStatusUseCase
{
    private readonly IPaymentStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdatePaymentStatusUseCase(IPaymentStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var paymentStatus = await _repository.GetByIdAsync(new PaymentStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"PaymentStatus with id {id} was not found.");

        paymentStatus.UpdateName(newName);
        await _repository.UpdateAsync(paymentStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PaymentStatus/Application/Services/PaymentStatusService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.UseCases;

public sealed class PaymentStatusService : IPaymentStatusService
{
    private readonly CreatePaymentStatusUseCase   _create;
    private readonly DeletePaymentStatusUseCase   _delete;
    private readonly GetAllPaymentStatusesUseCase _getAll;
    private readonly GetPaymentStatusByIdUseCase  _getById;
    private readonly UpdatePaymentStatusUseCase   _update;

    public PaymentStatusService(
        CreatePaymentStatusUseCase   create,
        DeletePaymentStatusUseCase   delete,
        GetAllPaymentStatusesUseCase getAll,
        GetPaymentStatusByIdUseCase  getById,
        UpdatePaymentStatusUseCase   update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<PaymentStatusDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new PaymentStatusDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<PaymentStatusDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new PaymentStatusDto(a.Id.Value, a.Name));
    }

    public async Task<PaymentStatusDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new PaymentStatusDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/PaymentStatus/Infrastructure/entity/PaymentStatusEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Infrastructure.Entity;

public sealed class PaymentStatusEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/PaymentStatus/Infrastructure/entity/PaymentStatusEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PaymentStatusEntityConfiguration : IEntityTypeConfiguration<PaymentStatusEntity>
{
    public void Configure(EntityTypeBuilder<PaymentStatusEntity> builder)
    {
        builder.ToTable("payment_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("payment_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_payment_status_name");
    }
}
```

---

### RUTA: `src/Modules/PaymentStatus/Infrastructure/repository/PaymentStatusRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class PaymentStatusRepository : IPaymentStatusRepository
{
    private readonly AppDbContext _context;

    public PaymentStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static PaymentStatusAggregate ToDomain(PaymentStatusEntity entity)
        => new(new PaymentStatusId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<PaymentStatusAggregate?> GetByIdAsync(
        PaymentStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<PaymentStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PaymentStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        PaymentStatusAggregate paymentStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = new PaymentStatusEntity { Name = paymentStatus.Name };
        await _context.PaymentStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        PaymentStatusAggregate paymentStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.PaymentStatuses
            .FirstOrDefaultAsync(e => e.Id == paymentStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentStatusEntity with id {paymentStatus.Id.Value} not found.");

        entity.Name = paymentStatus.Name;
        _context.PaymentStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        PaymentStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentStatusEntity with id {id.Value} not found.");

        _context.PaymentStatuses.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/PaymentStatus/UI/PaymentStatusConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentStatus.Application.Interfaces;

public sealed class PaymentStatusConsoleUI
{
    private readonly IPaymentStatusService _service;

    public PaymentStatusConsoleUI(IPaymentStatusService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== PAYMENT STATUS MODULE ==========");
            Console.WriteLine("1. List all payment statuses");
            Console.WriteLine("2. Get payment status by ID");
            Console.WriteLine("3. Create payment status");
            Console.WriteLine("4. Update payment status");
            Console.WriteLine("5. Delete payment status");
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
        Console.WriteLine("\n--- All Payment Statuses ---");

        foreach (var s in statuses)
            Console.WriteLine($"  [{s.Id}] {s.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter payment status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var status = await _service.GetByIdAsync(id);

        if (status is null)
            Console.WriteLine($"Payment status with ID {id} not found.");
        else
            Console.WriteLine($"  [{status.Id}] {status.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter payment status name (e.g. PENDING, PAID, REJECTED): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Payment status created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter payment status ID to update: ");
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
        Console.WriteLine("Payment status updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter payment status ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Payment status deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── PaymentStatus Module ──────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentStatusRepository, PaymentStatusRepository>();
builder.Services.AddScoped<CreatePaymentStatusUseCase>();
builder.Services.AddScoped<DeletePaymentStatusUseCase>();
builder.Services.AddScoped<GetAllPaymentStatusesUseCase>();
builder.Services.AddScoped<GetPaymentStatusByIdUseCase>();
builder.Services.AddScoped<UpdatePaymentStatusUseCase>();
builder.Services.AddScoped<IPaymentStatusService, PaymentStatusService>();
builder.Services.AddScoped<PaymentStatusConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/PaymentStatus/
├── Application/
│   ├── Interfaces/
│   │   └── IPaymentStatusService.cs
│   ├── Services/
│   │   └── PaymentStatusService.cs
│   └── UseCases/
│       ├── CreatePaymentStatusUseCase.cs
│       ├── DeletePaymentStatusUseCase.cs
│       ├── GetAllPaymentStatusesUseCase.cs
│       ├── GetPaymentStatusByIdUseCase.cs
│       └── UpdatePaymentStatusUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── PaymentStatusAggregate.cs
│   ├── Repositories/
│   │   └── IPaymentStatusRepository.cs
│   └── valueObject/
│       └── PaymentStatusId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── PaymentStatusEntity.cs
│   │   └── PaymentStatusEntityConfiguration.cs
│   └── repository/
│       └── PaymentStatusRepository.cs
└── UI/
    └── PaymentStatusConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo PaymentStatus_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
