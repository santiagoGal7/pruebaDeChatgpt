# Módulo: BaggageAllowance
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance`  
**Raíz de archivos:** `src/Modules/BaggageAllowance/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `baggage_allowance_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-9] |
| `cabin_class_id` | `INT NOT NULL FK` | `int` | FK → `cabin_class` |
| `fare_type_id` | `INT NOT NULL FK` | `int` | FK → `fare_type` |
| `carry_on_pieces` | `INT NOT NULL DEFAULT 1` | `int` | Artículos de mano incluidos |
| `carry_on_kg` | `DECIMAL(5,2) NOT NULL DEFAULT 10` | `decimal` | Peso máximo de mano |
| `checked_pieces` | `INT NOT NULL DEFAULT 0` | `int` | Maletas en bodega incluidas |
| `checked_kg` | `DECIMAL(5,2) NOT NULL DEFAULT 0` | `decimal` | Peso máximo en bodega |

**UNIQUE:** `(cabin_class_id, fare_type_id)` — una franquicia por combinación clase + tarifa.  
**4NF:** La combinación `(cabin_class_id, fare_type_id)` → todos los atributos. Economy-FLEX tiene distinta franquicia que Economy-PROMO — sin MVD independientes.  
Sin `created_at`, `updated_at` en el DDL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/BaggageAllowance/Domain/valueObject/BaggageAllowanceId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public sealed class BaggageAllowanceId
{
    public int Value { get; }

    public BaggageAllowanceId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("BaggageAllowanceId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is BaggageAllowanceId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Domain/aggregate/BaggageAllowanceAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

/// <summary>
/// Franquicia de equipaje incluida según la combinación clase de cabina + tipo de tarifa.
/// SQL: baggage_allowance. [NC-9] id renombrado a baggage_allowance_id.
///
/// 4NF: (cabin_class_id, fare_type_id) → carry_on_pieces, carry_on_kg,
///      checked_pieces, checked_kg. Sin MVD independientes.
/// UNIQUE: (cabin_class_id, fare_type_id).
///
/// Update(): modifica los límites de equipaje cuando cambia la política.
/// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class BaggageAllowanceAggregate
{
    public BaggageAllowanceId Id             { get; private set; }
    public int                CabinClassId   { get; private set; }
    public int                FareTypeId     { get; private set; }
    public int                CarryOnPieces  { get; private set; }
    public decimal            CarryOnKg      { get; private set; }
    public int                CheckedPieces  { get; private set; }
    public decimal            CheckedKg      { get; private set; }

    private BaggageAllowanceAggregate()
    {
        Id = null!;
    }

    public BaggageAllowanceAggregate(
        BaggageAllowanceId id,
        int                cabinClassId,
        int                fareTypeId,
        int                carryOnPieces,
        decimal            carryOnKg,
        int                checkedPieces,
        decimal            checkedKg)
    {
        if (cabinClassId <= 0)
            throw new ArgumentException("CabinClassId must be a positive integer.", nameof(cabinClassId));

        if (fareTypeId <= 0)
            throw new ArgumentException("FareTypeId must be a positive integer.", nameof(fareTypeId));

        ValidateLimits(carryOnPieces, carryOnKg, checkedPieces, checkedKg);

        Id            = id;
        CabinClassId  = cabinClassId;
        FareTypeId    = fareTypeId;
        CarryOnPieces = carryOnPieces;
        CarryOnKg     = carryOnKg;
        CheckedPieces = checkedPieces;
        CheckedKg     = checkedKg;
    }

    /// <summary>
    /// Actualiza los límites de la franquicia de equipaje.
    /// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
    /// </summary>
    public void Update(int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg)
    {
        ValidateLimits(carryOnPieces, carryOnKg, checkedPieces, checkedKg);

        CarryOnPieces = carryOnPieces;
        CarryOnKg     = carryOnKg;
        CheckedPieces = checkedPieces;
        CheckedKg     = checkedKg;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateLimits(
        int     carryOnPieces,
        decimal carryOnKg,
        int     checkedPieces,
        decimal checkedKg)
    {
        if (carryOnPieces < 0)
            throw new ArgumentException("CarryOnPieces must be >= 0.", nameof(carryOnPieces));

        if (carryOnKg < 0)
            throw new ArgumentException("CarryOnKg must be >= 0.", nameof(carryOnKg));

        if (checkedPieces < 0)
            throw new ArgumentException("CheckedPieces must be >= 0.", nameof(checkedPieces));

        if (checkedKg < 0)
            throw new ArgumentException("CheckedKg must be >= 0.", nameof(checkedKg));
    }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Domain/Repositories/IBaggageAllowanceRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public interface IBaggageAllowanceRepository
{
    Task<BaggageAllowanceAggregate?>             GetByIdAsync(BaggageAllowanceId id,                            CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageAllowanceAggregate>> GetAllAsync(                                                    CancellationToken cancellationToken = default);
    Task<BaggageAllowanceAggregate?>             GetByCabinAndFareAsync(int cabinClassId, int fareTypeId,        CancellationToken cancellationToken = default);
    Task                                         AddAsync(BaggageAllowanceAggregate baggageAllowance,           CancellationToken cancellationToken = default);
    Task                                         UpdateAsync(BaggageAllowanceAggregate baggageAllowance,        CancellationToken cancellationToken = default);
    Task                                         DeleteAsync(BaggageAllowanceId id,                             CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/BaggageAllowance/Application/Interfaces/IBaggageAllowanceService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;

public interface IBaggageAllowanceService
{
    Task<BaggageAllowanceDto?>             GetByIdAsync(int id,                                                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<BaggageAllowanceDto>> GetAllAsync(                                                                                      CancellationToken cancellationToken = default);
    Task<BaggageAllowanceDto?>             GetByCabinAndFareAsync(int cabinClassId, int fareTypeId,                                          CancellationToken cancellationToken = default);
    Task<BaggageAllowanceDto>              CreateAsync(int cabinClassId, int fareTypeId, int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg, CancellationToken cancellationToken = default);
    Task                                   UpdateAsync(int id, int carryOnPieces, decimal carryOnKg, int checkedPieces, decimal checkedKg,   CancellationToken cancellationToken = default);
    Task                                   DeleteAsync(int id,                                                                               CancellationToken cancellationToken = default);
}

public sealed record BaggageAllowanceDto(
    int     Id,
    int     CabinClassId,
    int     FareTypeId,
    int     CarryOnPieces,
    decimal CarryOnKg,
    int     CheckedPieces,
    decimal CheckedKg);
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/CreateBaggageAllowanceUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public CreateBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaggageAllowanceAggregate> ExecuteAsync(
        int               cabinClassId,
        int               fareTypeId,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        // BaggageAllowanceId(1) es placeholder; EF Core asigna el Id real al insertar.
        var baggageAllowance = new BaggageAllowanceAggregate(
            new BaggageAllowanceId(1),
            cabinClassId, fareTypeId,
            carryOnPieces, carryOnKg,
            checkedPieces, checkedKg);

        await _repository.AddAsync(baggageAllowance, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baggageAllowance;
    }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/DeleteBaggageAllowanceUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public DeleteBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaggageAllowanceId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/GetAllBaggageAllowancesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

public sealed class GetAllBaggageAllowancesUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetAllBaggageAllowancesUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaggageAllowanceAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/GetBaggageAllowanceByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public sealed class GetBaggageAllowanceByIdUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetBaggageAllowanceByIdUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageAllowanceAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaggageAllowanceId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/UpdateBaggageAllowanceUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza los límites de franquicia.
/// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class UpdateBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public UpdateBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        var baggageAllowance = await _repository.GetByIdAsync(new BaggageAllowanceId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaggageAllowance with id {id} was not found.");

        baggageAllowance.Update(carryOnPieces, carryOnKg, checkedPieces, checkedKg);
        await _repository.UpdateAsync(baggageAllowance, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/UseCases/GetBaggageAllowanceByCabinAndFareUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

/// <summary>
/// Consulta la franquicia para una combinación específica de clase + tarifa.
/// Caso de uso clave para informar al pasajero durante la reserva.
/// </summary>
public sealed class GetBaggageAllowanceByCabinAndFareUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetBaggageAllowanceByCabinAndFareUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageAllowanceAggregate?> ExecuteAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCabinAndFareAsync(cabinClassId, fareTypeId, cancellationToken);
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Application/Services/BaggageAllowanceService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;

public sealed class BaggageAllowanceService : IBaggageAllowanceService
{
    private readonly CreateBaggageAllowanceUseCase             _create;
    private readonly DeleteBaggageAllowanceUseCase             _delete;
    private readonly GetAllBaggageAllowancesUseCase            _getAll;
    private readonly GetBaggageAllowanceByIdUseCase            _getById;
    private readonly UpdateBaggageAllowanceUseCase             _update;
    private readonly GetBaggageAllowanceByCabinAndFareUseCase  _getByCabinAndFare;

    public BaggageAllowanceService(
        CreateBaggageAllowanceUseCase            create,
        DeleteBaggageAllowanceUseCase            delete,
        GetAllBaggageAllowancesUseCase           getAll,
        GetBaggageAllowanceByIdUseCase           getById,
        UpdateBaggageAllowanceUseCase            update,
        GetBaggageAllowanceByCabinAndFareUseCase getByCabinAndFare)
    {
        _create            = create;
        _delete            = delete;
        _getAll            = getAll;
        _getById           = getById;
        _update            = update;
        _getByCabinAndFare = getByCabinAndFare;
    }

    public async Task<BaggageAllowanceDto> CreateAsync(
        int               cabinClassId,
        int               fareTypeId,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            cabinClassId, fareTypeId, carryOnPieces, carryOnKg, checkedPieces, checkedKg, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<BaggageAllowanceDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<BaggageAllowanceDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, carryOnPieces, carryOnKg, checkedPieces, checkedKg, cancellationToken);

    public async Task<BaggageAllowanceDto?> GetByCabinAndFareAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByCabinAndFare.ExecuteAsync(cabinClassId, fareTypeId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static BaggageAllowanceDto ToDto(BaggageAllowanceAggregate agg)
        => new(
            agg.Id.Value,
            agg.CabinClassId,
            agg.FareTypeId,
            agg.CarryOnPieces,
            agg.CarryOnKg,
            agg.CheckedPieces,
            agg.CheckedKg);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/BaggageAllowance/Infrastructure/entity/BaggageAllowanceEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;

public sealed class BaggageAllowanceEntity
{
    public int     Id            { get; set; }
    public int     CabinClassId  { get; set; }
    public int     FareTypeId    { get; set; }
    public int     CarryOnPieces { get; set; }
    public decimal CarryOnKg     { get; set; }
    public int     CheckedPieces { get; set; }
    public decimal CheckedKg     { get; set; }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Infrastructure/entity/BaggageAllowanceEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaggageAllowanceEntityConfiguration : IEntityTypeConfiguration<BaggageAllowanceEntity>
{
    public void Configure(EntityTypeBuilder<BaggageAllowanceEntity> builder)
    {
        builder.ToTable("baggage_allowance");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("baggage_allowance_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.CabinClassId)
               .HasColumnName("cabin_class_id")
               .IsRequired();

        builder.Property(e => e.FareTypeId)
               .HasColumnName("fare_type_id")
               .IsRequired();

        // UNIQUE (cabin_class_id, fare_type_id) — espejo de uq_ba
        builder.HasIndex(e => new { e.CabinClassId, e.FareTypeId })
               .IsUnique()
               .HasDatabaseName("uq_ba");

        builder.Property(e => e.CarryOnPieces)
               .HasColumnName("carry_on_pieces")
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(e => e.CarryOnKg)
               .HasColumnName("carry_on_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)")
               .HasDefaultValue(10m);

        builder.Property(e => e.CheckedPieces)
               .HasColumnName("checked_pieces")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.CheckedKg)
               .HasColumnName("checked_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)")
               .HasDefaultValue(0m);
    }
}
```

---

### RUTA: `src/Modules/BaggageAllowance/Infrastructure/repository/BaggageAllowanceRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaggageAllowanceRepository : IBaggageAllowanceRepository
{
    private readonly AppDbContext _context;

    public BaggageAllowanceRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaggageAllowanceAggregate ToDomain(BaggageAllowanceEntity entity)
        => new(
            new BaggageAllowanceId(entity.Id),
            entity.CabinClassId,
            entity.FareTypeId,
            entity.CarryOnPieces,
            entity.CarryOnKg,
            entity.CheckedPieces,
            entity.CheckedKg);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaggageAllowanceAggregate?> GetByIdAsync(
        BaggageAllowanceId id,
        CancellationToken  cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaggageAllowanceAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaggageAllowances
            .AsNoTracking()
            .OrderBy(e => e.CabinClassId)
            .ThenBy(e => e.FareTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<BaggageAllowanceAggregate?> GetByCabinAndFareAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        // UNIQUE (cabin_class_id, fare_type_id) — FirstOrDefault es correcto.
        var entity = await _context.BaggageAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.CabinClassId == cabinClassId && e.FareTypeId == fareTypeId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        BaggageAllowanceAggregate baggageAllowance,
        CancellationToken         cancellationToken = default)
    {
        var entity = new BaggageAllowanceEntity
        {
            CabinClassId  = baggageAllowance.CabinClassId,
            FareTypeId    = baggageAllowance.FareTypeId,
            CarryOnPieces = baggageAllowance.CarryOnPieces,
            CarryOnKg     = baggageAllowance.CarryOnKg,
            CheckedPieces = baggageAllowance.CheckedPieces,
            CheckedKg     = baggageAllowance.CheckedKg
        };
        await _context.BaggageAllowances.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaggageAllowanceAggregate baggageAllowance,
        CancellationToken         cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .FirstOrDefaultAsync(e => e.Id == baggageAllowance.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageAllowanceEntity with id {baggageAllowance.Id.Value} not found.");

        // Solo los límites de equipaje son mutables.
        // CabinClassId y FareTypeId son la clave de negocio — inmutables.
        entity.CarryOnPieces = baggageAllowance.CarryOnPieces;
        entity.CarryOnKg     = baggageAllowance.CarryOnKg;
        entity.CheckedPieces = baggageAllowance.CheckedPieces;
        entity.CheckedKg     = baggageAllowance.CheckedKg;

        _context.BaggageAllowances.Update(entity);
    }

    public async Task DeleteAsync(
        BaggageAllowanceId id,
        CancellationToken  cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageAllowanceEntity with id {id.Value} not found.");

        _context.BaggageAllowances.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/BaggageAllowance/UI/BaggageAllowanceConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.Interfaces;

public sealed class BaggageAllowanceConsoleUI
{
    private readonly IBaggageAllowanceService _service;

    public BaggageAllowanceConsoleUI(IBaggageAllowanceService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== BAGGAGE ALLOWANCE MODULE ==========");
            Console.WriteLine("1. List all allowances");
            Console.WriteLine("2. Get allowance by ID");
            Console.WriteLine("3. Get allowance by cabin class + fare type");
            Console.WriteLine("4. Create allowance");
            Console.WriteLine("5. Update allowance limits");
            Console.WriteLine("6. Delete allowance");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();              break;
                case "2": await GetByIdAsync();              break;
                case "3": await GetByCabinAndFareAsync();    break;
                case "4": await CreateAsync();               break;
                case "5": await UpdateAsync();               break;
                case "6": await DeleteAsync();               break;
                case "0": running = false;                   break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var allowances = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Baggage Allowances ---");
        foreach (var a in allowances) PrintAllowance(a);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter allowance ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var a = await _service.GetByIdAsync(id);
        if (a is null) Console.WriteLine($"Allowance with ID {id} not found.");
        else           PrintAllowance(a);
    }

    private async Task GetByCabinAndFareAsync()
    {
        Console.Write("Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareId))
        { Console.WriteLine("Invalid ID."); return; }

        var a = await _service.GetByCabinAndFareAsync(cabinId, fareId);
        if (a is null)
            Console.WriteLine($"No allowance found for Cabin:{cabinId} / Fare:{fareId}.");
        else
            PrintAllowance(a);
    }

    private async Task CreateAsync()
    {
        Console.Write("Cabin Class ID: ");
        if (!int.TryParse(Console.ReadLine(), out int cabinId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Fare Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int fareId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Carry-on pieces (default 1): ");
        if (!int.TryParse(Console.ReadLine(), out int coPieces)) coPieces = 1;

        Console.Write("Carry-on kg (default 10): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal coKg)) coKg = 10m;

        Console.Write("Checked pieces (default 0): ");
        if (!int.TryParse(Console.ReadLine(), out int chkPieces)) chkPieces = 0;

        Console.Write("Checked kg (default 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal chkKg)) chkKg = 0m;

        try
        {
            var created = await _service.CreateAsync(
                cabinId, fareId, coPieces, coKg, chkPieces, chkKg);
            Console.WriteLine($"Allowance created: [{created.Id}]");
            PrintAllowance(created);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Allowance ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New carry-on pieces: ");
        if (!int.TryParse(Console.ReadLine(), out int coPieces)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New carry-on kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal coKg)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New checked pieces: ");
        if (!int.TryParse(Console.ReadLine(), out int chkPieces)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New checked kg: ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal chkKg)) { Console.WriteLine("Invalid."); return; }

        try
        {
            await _service.UpdateAsync(id, coPieces, coKg, chkPieces, chkKg);
            Console.WriteLine("Allowance updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Allowance ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Allowance deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintAllowance(BaggageAllowanceDto a)
        => Console.WriteLine(
            $"  [{a.Id}] Cabin:{a.CabinClassId} | Fare:{a.FareTypeId} | " +
            $"Carry-on: {a.CarryOnPieces}pcs {a.CarryOnKg:F1}kg | " +
            $"Checked: {a.CheckedPieces}pcs {a.CheckedKg:F1}kg");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── BaggageAllowance Module ───────────────────────────────────────────────────
builder.Services.AddScoped<IBaggageAllowanceRepository, BaggageAllowanceRepository>();
builder.Services.AddScoped<CreateBaggageAllowanceUseCase>();
builder.Services.AddScoped<DeleteBaggageAllowanceUseCase>();
builder.Services.AddScoped<GetAllBaggageAllowancesUseCase>();
builder.Services.AddScoped<GetBaggageAllowanceByIdUseCase>();
builder.Services.AddScoped<UpdateBaggageAllowanceUseCase>();
builder.Services.AddScoped<GetBaggageAllowanceByCabinAndFareUseCase>();
builder.Services.AddScoped<IBaggageAllowanceService, BaggageAllowanceService>();
builder.Services.AddScoped<BaggageAllowanceConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/BaggageAllowance/
├── Application/
│   ├── Interfaces/
│   │   └── IBaggageAllowanceService.cs
│   ├── Services/
│   │   └── BaggageAllowanceService.cs
│   └── UseCases/
│       ├── CreateBaggageAllowanceUseCase.cs
│       ├── DeleteBaggageAllowanceUseCase.cs
│       ├── GetAllBaggageAllowancesUseCase.cs
│       ├── GetBaggageAllowanceByCabinAndFareUseCase.cs
│       ├── GetBaggageAllowanceByIdUseCase.cs
│       └── UpdateBaggageAllowanceUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── BaggageAllowanceAggregate.cs
│   ├── Repositories/
│   │   └── IBaggageAllowanceRepository.cs
│   └── valueObject/
│       └── BaggageAllowanceId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── BaggageAllowanceEntity.cs
│   │   └── BaggageAllowanceEntityConfiguration.cs
│   └── repository/
│       └── BaggageAllowanceRepository.cs
└── UI/
    └── BaggageAllowanceConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo BaggageAllowance_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
