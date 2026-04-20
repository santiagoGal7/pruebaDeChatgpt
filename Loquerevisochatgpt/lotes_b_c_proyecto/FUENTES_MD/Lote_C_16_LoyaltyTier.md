# Módulo: LoyaltyTier
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier`  
**Raíz de archivos:** `src/Modules/LoyaltyTier/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `loyalty_tier_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-5] |
| `loyalty_program_id` | `INT NOT NULL FK` | `int` | FK → `loyalty_program` |
| `name` | `VARCHAR(50) NOT NULL` | `string` | Ej.: Classic, Silver, Gold, Diamond |
| `min_miles` | `INT NOT NULL DEFAULT 0` | `int` | Millas mínimas para alcanzar el tier |
| `benefits` | `TEXT NULL` | `string?` | Descripción de beneficios, nullable |

**UNIQUE:** `(loyalty_program_id, name)` — nombre único dentro del programa.  
**[IR-3]:** `UNIQUE (loyalty_program_id, loyalty_tier_id)` — soporte para la FK compuesta en `loyalty_account`.  
`min_miles >= 0` — validado en dominio.  
`loyalty_program_id` es la clave de contexto — inmutable.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/LoyaltyTier/Domain/valueObject/LoyaltyTierId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;

public sealed class LoyaltyTierId
{
    public int Value { get; }

    public LoyaltyTierId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("LoyaltyTierId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is LoyaltyTierId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Domain/aggregate/LoyaltyTierAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;

/// <summary>
/// Nivel del programa de fidelización (Classic, Silver, Gold, Diamond).
/// SQL: loyalty_tier. [NC-5] id renombrado a loyalty_tier_id.
///
/// UNIQUE: (loyalty_program_id, name) — nivel único por nombre dentro del programa.
/// UNIQUE: (loyalty_program_id, loyalty_tier_id) — [IR-3] soporte FK compuesta
///         en loyalty_account para garantizar que el tier pertenece al mismo programa.
///
/// Invariantes:
///   - min_miles >= 0.
///   - loyalty_program_id es la clave de contexto — inmutable.
///   - benefits es nullable (TEXT en SQL).
///
/// Update(): modifica nombre, millas mínimas y beneficios.
/// </summary>
public sealed class LoyaltyTierAggregate
{
    public LoyaltyTierId Id               { get; private set; }
    public int           LoyaltyProgramId { get; private set; }
    public string        Name             { get; private set; }
    public int           MinMiles         { get; private set; }
    public string?       Benefits         { get; private set; }

    private LoyaltyTierAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public LoyaltyTierAggregate(
        LoyaltyTierId id,
        int           loyaltyProgramId,
        string        name,
        int           minMiles,
        string?       benefits = null)
    {
        if (loyaltyProgramId <= 0)
            throw new ArgumentException(
                "LoyaltyProgramId must be a positive integer.", nameof(loyaltyProgramId));

        ValidateName(name);
        ValidateMinMiles(minMiles);

        Id               = id;
        LoyaltyProgramId = loyaltyProgramId;
        Name             = name.Trim();
        MinMiles         = minMiles;
        Benefits         = benefits?.Trim();
    }

    /// <summary>
    /// Actualiza el nombre, las millas mínimas y los beneficios del tier.
    /// loyalty_program_id es la clave de contexto — inmutable.
    /// </summary>
    public void Update(string name, int minMiles, string? benefits)
    {
        ValidateName(name);
        ValidateMinMiles(minMiles);

        Name     = name.Trim();
        MinMiles = minMiles;
        Benefits = benefits?.Trim();
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("LoyaltyTier name cannot be empty.", nameof(name));

        if (name.Trim().Length > 50)
            throw new ArgumentException(
                "LoyaltyTier name cannot exceed 50 characters.", nameof(name));
    }

    private static void ValidateMinMiles(int minMiles)
    {
        if (minMiles < 0)
            throw new ArgumentException(
                "MinMiles must be >= 0.", nameof(minMiles));
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Domain/Repositories/ILoyaltyTierRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;

public interface ILoyaltyTierRepository
{
    Task<LoyaltyTierAggregate?>             GetByIdAsync(LoyaltyTierId id,                              CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTierAggregate>> GetAllAsync(                                                 CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTierAggregate>> GetByProgramAsync(int loyaltyProgramId,                      CancellationToken cancellationToken = default);
    Task                                    AddAsync(LoyaltyTierAggregate loyaltyTier,                   CancellationToken cancellationToken = default);
    Task                                    UpdateAsync(LoyaltyTierAggregate loyaltyTier,                CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(LoyaltyTierId id,                                CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/LoyaltyTier/Application/Interfaces/ILoyaltyTierService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.Interfaces;

public interface ILoyaltyTierService
{
    Task<LoyaltyTierDto?>             GetByIdAsync(int id,                                                                    CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTierDto>> GetAllAsync(                                                                            CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTierDto>> GetByProgramAsync(int loyaltyProgramId,                                                 CancellationToken cancellationToken = default);
    Task<LoyaltyTierDto>              CreateAsync(int loyaltyProgramId, string name, int minMiles, string? benefits,          CancellationToken cancellationToken = default);
    Task                              UpdateAsync(int id, string name, int minMiles, string? benefits,                        CancellationToken cancellationToken = default);
    Task                              DeleteAsync(int id,                                                                     CancellationToken cancellationToken = default);
}

public sealed record LoyaltyTierDto(
    int     Id,
    int     LoyaltyProgramId,
    string  Name,
    int     MinMiles,
    string? Benefits);
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/CreateLoyaltyTierUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateLoyaltyTierUseCase
{
    private readonly ILoyaltyTierRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateLoyaltyTierUseCase(ILoyaltyTierRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyTierAggregate> ExecuteAsync(
        int               loyaltyProgramId,
        string            name,
        int               minMiles,
        string?           benefits,
        CancellationToken cancellationToken = default)
    {
        // LoyaltyTierId(1) es placeholder; EF Core asigna el Id real al insertar.
        var tier = new LoyaltyTierAggregate(
            new LoyaltyTierId(1), loyaltyProgramId, name, minMiles, benefits);

        await _repository.AddAsync(tier, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return tier;
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/DeleteLoyaltyTierUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteLoyaltyTierUseCase
{
    private readonly ILoyaltyTierRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteLoyaltyTierUseCase(ILoyaltyTierRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new LoyaltyTierId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/GetAllLoyaltyTiersUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;

public sealed class GetAllLoyaltyTiersUseCase
{
    private readonly ILoyaltyTierRepository _repository;

    public GetAllLoyaltyTiersUseCase(ILoyaltyTierRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyTierAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/GetLoyaltyTierByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;

public sealed class GetLoyaltyTierByIdUseCase
{
    private readonly ILoyaltyTierRepository _repository;

    public GetLoyaltyTierByIdUseCase(ILoyaltyTierRepository repository)
    {
        _repository = repository;
    }

    public async Task<LoyaltyTierAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new LoyaltyTierId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/UpdateLoyaltyTierUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateLoyaltyTierUseCase
{
    private readonly ILoyaltyTierRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateLoyaltyTierUseCase(ILoyaltyTierRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        int               minMiles,
        string?           benefits,
        CancellationToken cancellationToken = default)
    {
        var tier = await _repository.GetByIdAsync(new LoyaltyTierId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"LoyaltyTier with id {id} was not found.");

        tier.Update(name, minMiles, benefits);
        await _repository.UpdateAsync(tier, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/UseCases/GetLoyaltyTiersByProgramUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;

/// <summary>
/// Obtiene todos los niveles (tiers) de un programa de fidelización,
/// ordenados por millas mínimas ascendente (Classic → Diamond).
/// Caso de uso clave para mostrar la estructura de un programa.
/// </summary>
public sealed class GetLoyaltyTiersByProgramUseCase
{
    private readonly ILoyaltyTierRepository _repository;

    public GetLoyaltyTiersByProgramUseCase(ILoyaltyTierRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyTierAggregate>> ExecuteAsync(
        int               loyaltyProgramId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByProgramAsync(loyaltyProgramId, cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Application/Services/LoyaltyTierService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;

public sealed class LoyaltyTierService : ILoyaltyTierService
{
    private readonly CreateLoyaltyTierUseCase         _create;
    private readonly DeleteLoyaltyTierUseCase         _delete;
    private readonly GetAllLoyaltyTiersUseCase        _getAll;
    private readonly GetLoyaltyTierByIdUseCase        _getById;
    private readonly UpdateLoyaltyTierUseCase         _update;
    private readonly GetLoyaltyTiersByProgramUseCase  _getByProgram;

    public LoyaltyTierService(
        CreateLoyaltyTierUseCase        create,
        DeleteLoyaltyTierUseCase        delete,
        GetAllLoyaltyTiersUseCase       getAll,
        GetLoyaltyTierByIdUseCase       getById,
        UpdateLoyaltyTierUseCase        update,
        GetLoyaltyTiersByProgramUseCase getByProgram)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _update       = update;
        _getByProgram = getByProgram;
    }

    public async Task<LoyaltyTierDto> CreateAsync(
        int               loyaltyProgramId,
        string            name,
        int               minMiles,
        string?           benefits,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            loyaltyProgramId, name, minMiles, benefits, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<LoyaltyTierDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<LoyaltyTierDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string            name,
        int               minMiles,
        string?           benefits,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, name, minMiles, benefits, cancellationToken);

    public async Task<IEnumerable<LoyaltyTierDto>> GetByProgramAsync(
        int               loyaltyProgramId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByProgram.ExecuteAsync(loyaltyProgramId, cancellationToken);
        return list.Select(ToDto);
    }

    private static LoyaltyTierDto ToDto(LoyaltyTierAggregate agg)
        => new(agg.Id.Value, agg.LoyaltyProgramId, agg.Name, agg.MinMiles, agg.Benefits);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/LoyaltyTier/Infrastructure/entity/LoyaltyTierEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Infrastructure.Entity;

public sealed class LoyaltyTierEntity
{
    public int     Id               { get; set; }
    public int     LoyaltyProgramId { get; set; }
    public string  Name             { get; set; } = null!;
    public int     MinMiles         { get; set; }
    public string? Benefits         { get; set; }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Infrastructure/entity/LoyaltyTierEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class LoyaltyTierEntityConfiguration : IEntityTypeConfiguration<LoyaltyTierEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyTierEntity> builder)
    {
        builder.ToTable("loyalty_tier");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("loyalty_tier_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.LoyaltyProgramId)
               .HasColumnName("loyalty_program_id")
               .IsRequired();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        // UNIQUE (loyalty_program_id, name) — espejo de uq_lt
        builder.HasIndex(e => new { e.LoyaltyProgramId, e.Name })
               .IsUnique()
               .HasDatabaseName("uq_lt");

        // UNIQUE (loyalty_program_id, loyalty_tier_id) — espejo de uq_lt_fk [IR-3]
        // Soporte para FK compuesta en loyalty_account
        builder.HasIndex(e => new { e.LoyaltyProgramId, e.Id })
               .IsUnique()
               .HasDatabaseName("uq_lt_fk");

        builder.Property(e => e.MinMiles)
               .HasColumnName("min_miles")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.Benefits)
               .HasColumnName("benefits")
               .IsRequired(false);
        // TEXT en MySQL — sin HasMaxLength para dejar que EF use el tipo nativo
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTier/Infrastructure/repository/LoyaltyTierRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class LoyaltyTierRepository : ILoyaltyTierRepository
{
    private readonly AppDbContext _context;

    public LoyaltyTierRepository(AppDbContext context)
    {
        _context = context;
    }

    private static LoyaltyTierAggregate ToDomain(LoyaltyTierEntity entity)
        => new(
            new LoyaltyTierId(entity.Id),
            entity.LoyaltyProgramId,
            entity.Name,
            entity.MinMiles,
            entity.Benefits);

    public async Task<LoyaltyTierAggregate?> GetByIdAsync(
        LoyaltyTierId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyTiers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<LoyaltyTierAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyTiers
            .AsNoTracking()
            .OrderBy(e => e.LoyaltyProgramId)
            .ThenBy(e => e.MinMiles)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<LoyaltyTierAggregate>> GetByProgramAsync(
        int               loyaltyProgramId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyTiers
            .AsNoTracking()
            .Where(e => e.LoyaltyProgramId == loyaltyProgramId)
            .OrderBy(e => e.MinMiles)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        LoyaltyTierAggregate loyaltyTier,
        CancellationToken    cancellationToken = default)
    {
        var entity = new LoyaltyTierEntity
        {
            LoyaltyProgramId = loyaltyTier.LoyaltyProgramId,
            Name             = loyaltyTier.Name,
            MinMiles         = loyaltyTier.MinMiles,
            Benefits         = loyaltyTier.Benefits
        };
        await _context.LoyaltyTiers.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        LoyaltyTierAggregate loyaltyTier,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.LoyaltyTiers
            .FirstOrDefaultAsync(e => e.Id == loyaltyTier.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyTierEntity with id {loyaltyTier.Id.Value} not found.");

        // LoyaltyProgramId es la clave de contexto — inmutable.
        entity.Name     = loyaltyTier.Name;
        entity.MinMiles = loyaltyTier.MinMiles;
        entity.Benefits = loyaltyTier.Benefits;

        _context.LoyaltyTiers.Update(entity);
    }

    public async Task DeleteAsync(
        LoyaltyTierId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyTiers
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyTierEntity with id {id.Value} not found.");

        _context.LoyaltyTiers.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/LoyaltyTier/UI/LoyaltyTierConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTier.Application.Interfaces;

public sealed class LoyaltyTierConsoleUI
{
    private readonly ILoyaltyTierService _service;

    public LoyaltyTierConsoleUI(ILoyaltyTierService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== LOYALTY TIER MODULE ==========");
            Console.WriteLine("1. List all tiers");
            Console.WriteLine("2. Get tier by ID");
            Console.WriteLine("3. List tiers by program");
            Console.WriteLine("4. Create tier");
            Console.WriteLine("5. Update tier");
            Console.WriteLine("6. Delete tier");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();        break;
                case "2": await GetByIdAsync();        break;
                case "3": await ListByProgramAsync();  break;
                case "4": await CreateAsync();         break;
                case "5": await UpdateAsync();         break;
                case "6": await DeleteAsync();         break;
                case "0": running = false;             break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private async Task ListAllAsync()
    {
        var tiers = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Loyalty Tiers ---");
        foreach (var t in tiers) PrintTier(t);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter tier ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByIdAsync(id);
        if (t is null) Console.WriteLine($"Tier with ID {id} not found.");
        else           PrintTier(t);
    }

    private async Task ListByProgramAsync()
    {
        Console.Write("Enter Loyalty Program ID: ");
        if (!int.TryParse(Console.ReadLine(), out int programId))
        { Console.WriteLine("Invalid ID."); return; }

        var tiers = await _service.GetByProgramAsync(programId);
        Console.WriteLine($"\n--- Tiers for Program {programId} ---");
        foreach (var t in tiers) PrintTier(t);
    }

    private async Task CreateAsync()
    {
        Console.Write("Loyalty Program ID: ");
        if (!int.TryParse(Console.ReadLine(), out int programId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Tier name (e.g. Classic, Silver, Gold, Diamond): ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("Minimum miles (>= 0, default 0): ");
        if (!int.TryParse(Console.ReadLine(), out int minMiles) || minMiles < 0)
            minMiles = 0;

        Console.Write("Benefits description (optional): ");
        var benefitsInput = Console.ReadLine()?.Trim();
        string? benefits = string.IsNullOrWhiteSpace(benefitsInput) ? null : benefitsInput;

        try
        {
            var created = await _service.CreateAsync(programId, name, minMiles, benefits);
            Console.WriteLine($"Tier created: [{created.Id}]");
            PrintTier(created);
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Tier ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name cannot be empty."); return; }

        Console.Write("New minimum miles (>= 0): ");
        if (!int.TryParse(Console.ReadLine(), out int minMiles) || minMiles < 0)
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New benefits (optional, Enter to clear): ");
        var benefitsInput = Console.ReadLine()?.Trim();
        string? benefits = string.IsNullOrWhiteSpace(benefitsInput) ? null : benefitsInput;

        try
        {
            await _service.UpdateAsync(id, name, minMiles, benefits);
            Console.WriteLine("Tier updated successfully.");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Tier ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Tier deleted successfully.");
    }

    private static void PrintTier(LoyaltyTierDto t)
        => Console.WriteLine(
            $"  [{t.Id}] [{t.LoyaltyProgramId}] {t.Name} | Min: {t.MinMiles:N0} miles" +
            (t.Benefits is not null ? $" | {t.Benefits[..Math.Min(50, t.Benefits.Length)]}..." : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento)_

```csharp
// ── LoyaltyTier Module ────────────────────────────────────────────────────────
builder.Services.AddScoped<ILoyaltyTierRepository, LoyaltyTierRepository>();
builder.Services.AddScoped<CreateLoyaltyTierUseCase>();
builder.Services.AddScoped<DeleteLoyaltyTierUseCase>();
builder.Services.AddScoped<GetAllLoyaltyTiersUseCase>();
builder.Services.AddScoped<GetLoyaltyTierByIdUseCase>();
builder.Services.AddScoped<UpdateLoyaltyTierUseCase>();
builder.Services.AddScoped<GetLoyaltyTiersByProgramUseCase>();
builder.Services.AddScoped<ILoyaltyTierService, LoyaltyTierService>();
builder.Services.AddScoped<LoyaltyTierConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/LoyaltyTier/
├── Application/
│   ├── Interfaces/
│   │   └── ILoyaltyTierService.cs
│   ├── Services/
│   │   └── LoyaltyTierService.cs
│   └── UseCases/
│       ├── CreateLoyaltyTierUseCase.cs
│       ├── DeleteLoyaltyTierUseCase.cs
│       ├── GetAllLoyaltyTiersUseCase.cs
│       ├── GetLoyaltyTierByIdUseCase.cs
│       ├── GetLoyaltyTiersByProgramUseCase.cs
│       └── UpdateLoyaltyTierUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── LoyaltyTierAggregate.cs
│   ├── Repositories/
│   │   └── ILoyaltyTierRepository.cs
│   └── valueObject/
│       └── LoyaltyTierId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── LoyaltyTierEntity.cs
│   │   └── LoyaltyTierEntityConfiguration.cs
│   └── repository/
│       └── LoyaltyTierRepository.cs
└── UI/
    └── LoyaltyTierConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo LoyaltyTier_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
