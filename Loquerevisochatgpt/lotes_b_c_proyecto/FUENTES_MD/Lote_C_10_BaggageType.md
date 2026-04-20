# Módulo: BaggageType
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType`  
**Raíz de archivos:** `src/Modules/BaggageType/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `baggage_type_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `name` | `VARCHAR(80) NOT NULL UNIQUE` | `string` | Ej.: STANDARD_CHECKED, OVERSIZE, FRAGILE |
| `max_weight_kg` | `DECIMAL(5,2) NOT NULL` | `decimal` | Peso máximo permitido |
| `extra_fee` | `DECIMAL(10,2) NOT NULL DEFAULT 0` | `decimal` | CHECK `>= 0` — tarifa adicional |

**CHECK:** `extra_fee >= 0` — espejado en el dominio.  
Sin `created_at`, `updated_at` en el DDL.  
Tabla de tipos de equipaje adicional (por encima de la franquicia incluida).

---

## 1. DOMAIN

---

### RUTA: `src/Modules/BaggageType/Domain/valueObject/BaggageTypeId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

public sealed class BaggageTypeId
{
    public int Value { get; }

    public BaggageTypeId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BaggageTypeId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BaggageTypeId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/BaggageType/Domain/aggregate/BaggageTypeAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

/// <summary>
/// Tipo de equipaje adicional cobrable (por encima de la franquicia incluida).
/// SQL: baggage_type.
///
/// Invariantes:
///   - name: máximo 80 caracteres, UNIQUE, normalizado a mayúsculas.
///   - max_weight_kg: peso máximo para este tipo de equipaje.
///   - extra_fee >= 0 (espejo del chk_bt_fee).
///
/// Update(): modifica nombre, peso máximo y tarifa.
/// </summary>
public sealed class BaggageTypeAggregate
{
    public BaggageTypeId Id           { get; private set; }
    public string        Name         { get; private set; }
    public decimal       MaxWeightKg  { get; private set; }
    public decimal       ExtraFee     { get; private set; }

    private BaggageTypeAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public BaggageTypeAggregate(
        BaggageTypeId id,
        string        name,
        decimal       maxWeightKg,
        decimal       extraFee)
    {
        ValidateName(name);
        ValidateMaxWeightKg(maxWeightKg);
        ValidateExtraFee(extraFee);

        Id          = id;
        Name        = name.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        ExtraFee    = extraFee;
    }

    public void Update(string name, decimal maxWeightKg, decimal extraFee)
    {
        ValidateName(name);
        ValidateMaxWeightKg(maxWeightKg);
        ValidateExtraFee(extraFee);

        Name        = name.Trim().ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        ExtraFee    = extraFee;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("BaggageType name cannot be empty.", nameof(name));

        if (name.Trim().Length > 80)
            throw new ArgumentException(
                "BaggageType name cannot exceed 80 characters.", nameof(name));
    }

    private static void ValidateMaxWeightKg(decimal maxWeightKg)
    {
        if (maxWeightKg <= 0)
            throw new ArgumentException(
                "MaxWeightKg must be a positive value.", nameof(maxWeightKg));
    }

    private static void ValidateExtraFee(decimal extraFee)
    {
        if (extraFee < 0)
            throw new ArgumentException(
                "ExtraFee must be >= 0. [chk_bt_fee]", nameof(extraFee));
    }
}
```

---

### RUTA: `src/Modules/BaggageType/Domain/Repositories/IBaggageTypeRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

public interface IBaggageTypeRepository
{
    Task<BaggageTypeAggregate?>             GetByIdAsync(BaggageTypeId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageTypeAggregate>> GetAllAsync(                                  CancellationToken cancellationToken = default);
    Task                                    AddAsync(BaggageTypeAggregate baggageType,    CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(BaggageTypeAggregate baggageType, CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(BaggageTypeId id,                 CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/BaggageType/Application/Interfaces/IBaggageTypeService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;

public interface IBaggageTypeService
{
    Task<BaggageTypeDto?>             GetByIdAsync(int id,                                                    CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageTypeDto>> GetAllAsync(                                                            CancellationToken cancellationToken = default);
    Task<BaggageTypeDto>              CreateAsync(string name, decimal maxWeightKg, decimal extraFee,        CancellationToken cancellationToken = default);
    Task                              UpdateAsync(int id, string name, decimal maxWeightKg, decimal extraFee, CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                                                     CancellationToken cancellationToken = default);
}

public sealed record BaggageTypeDto(int Id, string Name, decimal MaxWeightKg, decimal ExtraFee);
```

---

### RUTA: `src/Modules/BaggageType/Application/UseCases/CreateBaggageTypeUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaggageTypeAggregate> ExecuteAsync(
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        // BaggageTypeId(1) es placeholder; EF Core asigna el Id real al insertar.
        var baggageType = new BaggageTypeAggregate(
            new BaggageTypeId(1), name, maxWeightKg, extraFee);

        await _repository.AddAsync(baggageType, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baggageType;
    }
}
```

---

### RUTA: `src/Modules/BaggageType/Application/UseCases/DeleteBaggageTypeUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaggageTypeId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaggageType/Application/UseCases/GetAllBaggageTypesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;

public sealed class GetAllBaggageTypesUseCase
{
    private readonly IBaggageTypeRepository _repository;

    public GetAllBaggageTypesUseCase(IBaggageTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaggageTypeAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/BaggageType/Application/UseCases/GetBaggageTypeByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

public sealed class GetBaggageTypeByIdUseCase
{
    private readonly IBaggageTypeRepository _repository;

    public GetBaggageTypeByIdUseCase(IBaggageTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageTypeAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaggageTypeId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/BaggageType/Application/UseCases/UpdateBaggageTypeUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        var baggageType = await _repository.GetByIdAsync(new BaggageTypeId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaggageType with id {id} was not found.");

        baggageType.Update(name, maxWeightKg, extraFee);
        await _repository.UpdateAsync(baggageType, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaggageType/Application/Services/BaggageTypeService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

public sealed class BaggageTypeService : IBaggageTypeService
{
    private readonly CreateBaggageTypeUseCase   _create;
    private readonly DeleteBaggageTypeUseCase   _delete;
    private readonly GetAllBaggageTypesUseCase  _getAll;
    private readonly GetBaggageTypeByIdUseCase  _getById;
    private readonly UpdateBaggageTypeUseCase   _update;

    public BaggageTypeService(
        CreateBaggageTypeUseCase  create,
        DeleteBaggageTypeUseCase  delete,
        GetAllBaggageTypesUseCase getAll,
        GetBaggageTypeByIdUseCase getById,
        UpdateBaggageTypeUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<BaggageTypeDto> CreateAsync(
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(name, maxWeightKg, extraFee, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaggageTypeDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaggageTypeDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, maxWeightKg, extraFee, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaggageTypeDto ToDto(
        Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate.BaggageTypeAggregate agg)
        => new(agg.Id.Value, agg.Name, agg.MaxWeightKg, agg.ExtraFee);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/BaggageType/Infrastructure/entity/BaggageTypeEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;

public sealed class BaggageTypeEntity
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = null!;
    public decimal MaxWeightKg { get; set; }
    public decimal ExtraFee    { get; set; }
}
```

---

### RUTA: `src/Modules/BaggageType/Infrastructure/entity/BaggageTypeEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaggageTypeEntityConfiguration : IEntityTypeConfiguration<BaggageTypeEntity>
{
    public void Configure(EntityTypeBuilder<BaggageTypeEntity> builder)
    {
        builder.ToTable("baggage_type");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("baggage_type_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_baggage_type_name");

        builder.Property(e => e.MaxWeightKg)
               .HasColumnName("max_weight_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)");

        builder.Property(e => e.ExtraFee)
               .HasColumnName("extra_fee")
               .IsRequired()
               .HasColumnType("decimal(10,2)")
               .HasDefaultValue(0m);
    }
}
```

---

### RUTA: `src/Modules/BaggageType/Infrastructure/repository/BaggageTypeRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaggageTypeRepository : IBaggageTypeRepository
{
    private readonly AppDbContext _context;

    public BaggageTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaggageTypeAggregate ToDomain(BaggageTypeEntity entity)
        => new(new BaggageTypeId(entity.Id), entity.Name, entity.MaxWeightKg, entity.ExtraFee);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaggageTypeAggregate?> GetByIdAsync(
        BaggageTypeId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaggageTypeAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaggageTypes
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        BaggageTypeAggregate baggageType,
        CancellationToken    cancellationToken = default)
    {
        var entity = new BaggageTypeEntity
        {
            Name        = baggageType.Name,
            MaxWeightKg = baggageType.MaxWeightKg,
            ExtraFee    = baggageType.ExtraFee
        };
        await _context.BaggageTypes.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaggageTypeAggregate baggageType,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .FirstOrDefaultAsync(e => e.Id == baggageType.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageTypeEntity with id {baggageType.Id.Value} not found.");

        entity.Name        = baggageType.Name;
        entity.MaxWeightKg = baggageType.MaxWeightKg;
        entity.ExtraFee    = baggageType.ExtraFee;

        _context.BaggageTypes.Update(entity);
    }

    public async Task DeleteAsync(
        BaggageTypeId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageTypeEntity with id {id.Value} not found.");

        _context.BaggageTypes.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/BaggageType/UI/BaggageTypeConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.Interfaces;

public sealed class BaggageTypeConsoleUI
{
    private readonly IBaggageTypeService _service;

    public BaggageTypeConsoleUI(IBaggageTypeService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BAGGAGE TYPE MODULE ==========");
            Console.WriteLine("1. List all baggage types");
            Console.WriteLine("2. Get baggage type by ID");
            Console.WriteLine("3. Create baggage type");
            Console.WriteLine("4. Update baggage type");
            Console.WriteLine("5. Delete baggage type");
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
        var types = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Baggage Types ---");

        foreach (var t in types)
            Console.WriteLine(
                $"  [{t.Id}] {t.Name} | Max:{t.MaxWeightKg:F1}kg | Fee:{t.ExtraFee:F2}");
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter baggage type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByIdAsync(id);
        if (t is null)
            Console.WriteLine($"Baggage type with ID {id} not found.");
        else
            Console.WriteLine(
                $"  [{t.Id}] {t.Name} | Max:{t.MaxWeightKg:F1}kg | Fee:{t.ExtraFee:F2}");
    }

    private async Task CreateAsync()
    {
        Console.Write("Name (e.g. STANDARD_CHECKED, OVERSIZE, FRAGILE): ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Max weight kg (e.g. 23.00): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal maxKg))
        { Console.WriteLine("Invalid weight."); return; }

        Console.Write("Extra fee (>= 0, default 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
            fee = 0m;

        try
        {
            var created = await _service.CreateAsync(name, maxKg, fee);
            Console.WriteLine(
                $"Baggage type created: [{created.Id}] {created.Name} | " +
                $"Max:{created.MaxWeightKg:F1}kg | Fee:{created.ExtraFee:F2}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Baggage type ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New max weight kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal maxKg))
        { Console.WriteLine("Invalid weight."); return; }

        Console.Write("New extra fee (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
        { Console.WriteLine("Invalid fee."); return; }

        try
        {
            await _service.UpdateAsync(id, name, maxKg, fee);
            Console.WriteLine("Baggage type updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Baggage type ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Baggage type deleted successfully.");
    }
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── BaggageType Module ────────────────────────────────────────────────────────
builder.Services.AddScoped<IBaggageTypeRepository, BaggageTypeRepository>();
builder.Services.AddScoped<CreateBaggageTypeUseCase>();
builder.Services.AddScoped<DeleteBaggageTypeUseCase>();
builder.Services.AddScoped<GetAllBaggageTypesUseCase>();
builder.Services.AddScoped<GetBaggageTypeByIdUseCase>();
builder.Services.AddScoped<UpdateBaggageTypeUseCase>();
builder.Services.AddScoped<IBaggageTypeService, BaggageTypeService>();
builder.Services.AddScoped<BaggageTypeConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/BaggageType/
├── Application/
│   ├── Interfaces/
│   │   └── IBaggageTypeService.cs
│   ├── Services/
│   │   └── BaggageTypeService.cs
│   └── UseCases/
│       ├── CreateBaggageTypeUseCase.cs
│       ├── DeleteBaggageTypeUseCase.cs
│       ├── GetAllBaggageTypesUseCase.cs
│       ├── GetBaggageTypeByIdUseCase.cs
│       └── UpdateBaggageTypeUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── BaggageTypeAggregate.cs
│   ├── Repositories/
│   │   └── IBaggageTypeRepository.cs
│   └── valueObject/
│       └── BaggageTypeId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── BaggageTypeEntity.cs
│   │   └── BaggageTypeEntityConfiguration.cs
│   └── repository/
│       └── BaggageTypeRepository.cs
└── UI/
    └── BaggageTypeConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo BaggageType_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
