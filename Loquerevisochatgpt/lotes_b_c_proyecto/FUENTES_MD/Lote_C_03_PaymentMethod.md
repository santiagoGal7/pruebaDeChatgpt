# Módulo: PaymentMethod
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod`  
**Raíz de archivos:** `src/Modules/PaymentMethod/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `payment_method_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(50) NOT NULL UNIQUE` | `string` | Catálogo: CREDIT_CARD, DEBIT_CARD, CASH, TRANSFER |

Tabla catálogo mínima. Sin `created_at`, `updated_at` ni FKs en el DDL.  
Nombre normalizado a `ToUpperInvariant()` para consistencia con el catálogo SQL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/PaymentMethod/Domain/valueObject/PaymentMethodId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;

public sealed class PaymentMethodId
{
    public int Value { get; }

    public PaymentMethodId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("PaymentMethodId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is PaymentMethodId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/PaymentMethod/Domain/aggregate/PaymentMethodAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;

/// <summary>
/// Catálogo de métodos de pago aceptados.
/// Valores esperados: CREDIT_CARD, DEBIT_CARD, CASH, TRANSFER.
/// Nombre normalizado a mayúsculas para consistencia con el catálogo SQL.
/// </summary>
public sealed class PaymentMethodAggregate
{
    public PaymentMethodId Id   { get; private set; }
    public string          Name { get; private set; }

    private PaymentMethodAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public PaymentMethodAggregate(PaymentMethodId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("PaymentMethod name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "PaymentMethod name cannot exceed 50 characters.", nameof(name));

        Id   = id;
        Name = name.Trim().ToUpperInvariant();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("PaymentMethod name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 50)
            throw new ArgumentException(
                "PaymentMethod name cannot exceed 50 characters.", nameof(newName));

        Name = newName.Trim().ToUpperInvariant();
    }
}
```

---

### RUTA: `src/Modules/PaymentMethod/Domain/Repositories/IPaymentMethodRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;

public interface IPaymentMethodRepository
{
    Task<PaymentMethodAggregate?>             GetByIdAsync(PaymentMethodId id,                 CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentMethodAggregate>> GetAllAsync(                                      CancellationToken cancellationToken = default);
    Task                                      AddAsync(PaymentMethodAggregate paymentMethod,    CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(PaymentMethodAggregate paymentMethod, CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(PaymentMethodId id,                   CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/PaymentMethod/Application/Interfaces/IPaymentMethodService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.Interfaces;

public interface IPaymentMethodService
{
    Task<PaymentMethodDto?>             GetByIdAsync(int id,            CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentMethodDto>> GetAllAsync(                    CancellationToken cancellationToken = default);
    Task<PaymentMethodDto>              CreateAsync(string name,        CancellationToken cancellationToken = default);
    Task                                UpdateAsync(int id, string name,CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,             CancellationToken cancellationToken = default);
}

public sealed record PaymentMethodDto(int Id, string Name);
```

---

### RUTA: `src/Modules/PaymentMethod/Application/UseCases/CreatePaymentMethodUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreatePaymentMethodUseCase
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreatePaymentMethodUseCase(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentMethodAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // PaymentMethodId(1) es placeholder; EF Core asigna el Id real al insertar.
        var paymentMethod = new PaymentMethodAggregate(new PaymentMethodId(1), name);

        await _repository.AddAsync(paymentMethod, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return paymentMethod;
    }
}
```

---

### RUTA: `src/Modules/PaymentMethod/Application/UseCases/DeletePaymentMethodUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeletePaymentMethodUseCase
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeletePaymentMethodUseCase(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new PaymentMethodId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PaymentMethod/Application/UseCases/GetAllPaymentMethodsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;

public sealed class GetAllPaymentMethodsUseCase
{
    private readonly IPaymentMethodRepository _repository;

    public GetAllPaymentMethodsUseCase(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentMethodAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/PaymentMethod/Application/UseCases/GetPaymentMethodByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;

public sealed class GetPaymentMethodByIdUseCase
{
    private readonly IPaymentMethodRepository _repository;

    public GetPaymentMethodByIdUseCase(IPaymentMethodRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentMethodAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new PaymentMethodId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/PaymentMethod/Application/UseCases/UpdatePaymentMethodUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdatePaymentMethodUseCase
{
    private readonly IPaymentMethodRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdatePaymentMethodUseCase(IPaymentMethodRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = await _repository.GetByIdAsync(new PaymentMethodId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"PaymentMethod with id {id} was not found.");

        paymentMethod.UpdateName(newName);
        await _repository.UpdateAsync(paymentMethod, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PaymentMethod/Application/Services/PaymentMethodService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.UseCases;

public sealed class PaymentMethodService : IPaymentMethodService
{
    private readonly CreatePaymentMethodUseCase   _create;
    private readonly DeletePaymentMethodUseCase   _delete;
    private readonly GetAllPaymentMethodsUseCase  _getAll;
    private readonly GetPaymentMethodByIdUseCase  _getById;
    private readonly UpdatePaymentMethodUseCase   _update;

    public PaymentMethodService(
        CreatePaymentMethodUseCase  create,
        DeletePaymentMethodUseCase  delete,
        GetAllPaymentMethodsUseCase getAll,
        GetPaymentMethodByIdUseCase getById,
        UpdatePaymentMethodUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<PaymentMethodDto> CreateAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, cancellationToken);
        return new PaymentMethodDto(agg.Id.Value, agg.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<PaymentMethodDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(a => new PaymentMethodDto(a.Id.Value, a.Name));
    }

    public async Task<PaymentMethodDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : new PaymentMethodDto(agg.Id.Value, agg.Name);
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

### RUTA: `src/Modules/PaymentMethod/Infrastructure/entity/PaymentMethodEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Infrastructure.Entity;

public sealed class PaymentMethodEntity
{
    public int    Id   { get; set; }
    public string Name { get; set; } = null!;
}
```

---

### RUTA: `src/Modules/PaymentMethod/Infrastructure/entity/PaymentMethodEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PaymentMethodEntityConfiguration : IEntityTypeConfiguration<PaymentMethodEntity>
{
    public void Configure(EntityTypeBuilder<PaymentMethodEntity> builder)
    {
        builder.ToTable("payment_method");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("payment_method_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_payment_method_name");
    }
}
```

---

### RUTA: `src/Modules/PaymentMethod/Infrastructure/repository/PaymentMethodRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly AppDbContext _context;

    public PaymentMethodRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static PaymentMethodAggregate ToDomain(PaymentMethodEntity entity)
        => new(new PaymentMethodId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<PaymentMethodAggregate?> GetByIdAsync(
        PaymentMethodId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<PaymentMethodAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PaymentMethods
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        PaymentMethodAggregate paymentMethod,
        CancellationToken      cancellationToken = default)
    {
        var entity = new PaymentMethodEntity { Name = paymentMethod.Name };
        await _context.PaymentMethods.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        PaymentMethodAggregate paymentMethod,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.PaymentMethods
            .FirstOrDefaultAsync(e => e.Id == paymentMethod.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentMethodEntity with id {paymentMethod.Id.Value} not found.");

        entity.Name = paymentMethod.Name;
        _context.PaymentMethods.Update(entity);
    }

    public async Task DeleteAsync(
        PaymentMethodId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.PaymentMethods
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PaymentMethodEntity with id {id.Value} not found.");

        _context.PaymentMethods.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/PaymentMethod/UI/PaymentMethodConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PaymentMethod.Application.Interfaces;

public sealed class PaymentMethodConsoleUI
{
    private readonly IPaymentMethodService _service;

    public PaymentMethodConsoleUI(IPaymentMethodService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== PAYMENT METHOD MODULE ==========");
            Console.WriteLine("1. List all payment methods");
            Console.WriteLine("2. Get payment method by ID");
            Console.WriteLine("3. Create payment method");
            Console.WriteLine("4. Update payment method");
            Console.WriteLine("5. Delete payment method");
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
        var methods = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Payment Methods ---");

        foreach (var m in methods)
            Console.WriteLine($"  [{m.Id}] {m.Name}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter payment method ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var method = await _service.GetByIdAsync(id);

        if (method is null)
            Console.WriteLine($"Payment method with ID {id} not found.");
        else
            Console.WriteLine($"  [{method.Id}] {method.Name}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Enter payment method name (e.g. CREDIT_CARD, DEBIT_CARD, CASH, TRANSFER): ");
        var name = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name cannot be empty.");
            return;
        }

        var created = await _service.CreateAsync(name);
        Console.WriteLine($"Payment method created: [{created.Id}] {created.Name}");
    }

    private async Task UpdateAsync()
    {
        Console.Write("Enter payment method ID to update: ");
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
        Console.WriteLine("Payment method updated successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Enter payment method ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        await _service.DeleteAsync(id);
        Console.WriteLine("Payment method deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── PaymentMethod Module ──────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<CreatePaymentMethodUseCase>();
builder.Services.AddScoped<DeletePaymentMethodUseCase>();
builder.Services.AddScoped<GetAllPaymentMethodsUseCase>();
builder.Services.AddScoped<GetPaymentMethodByIdUseCase>();
builder.Services.AddScoped<UpdatePaymentMethodUseCase>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<PaymentMethodConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/PaymentMethod/
├── Application/
│   ├── Interfaces/
│   │   └── IPaymentMethodService.cs
│   ├── Services/
│   │   └── PaymentMethodService.cs
│   └── UseCases/
│       ├── CreatePaymentMethodUseCase.cs
│       ├── DeletePaymentMethodUseCase.cs
│       ├── GetAllPaymentMethodsUseCase.cs
│       ├── GetPaymentMethodByIdUseCase.cs
│       └── UpdatePaymentMethodUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── PaymentMethodAggregate.cs
│   ├── Repositories/
│   │   └── IPaymentMethodRepository.cs
│   └── valueObject/
│       └── PaymentMethodId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── PaymentMethodEntity.cs
│   │   └── PaymentMethodEntityConfiguration.cs
│   └── repository/
│       └── PaymentMethodRepository.cs
└── UI/
    └── PaymentMethodConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo PaymentMethod_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
