# Módulo: LoyaltyAccount
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount`  
**Raíz de archivos:** `src/Modules/LoyaltyAccount/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `loyalty_account_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-4] |
| `passenger_id` | `INT NOT NULL FK` | `int` | FK → `passenger` |
| `loyalty_program_id` | `INT NOT NULL FK` | `int` | FK → `loyalty_program` |
| `loyalty_tier_id` | `INT NOT NULL FK` | `int` | FK compuesta `(loyalty_program_id, loyalty_tier_id)` → `loyalty_tier` [IR-3] |
| `total_miles` | `INT NOT NULL DEFAULT 0` | `int` | Millas acumuladas históricas |
| `available_miles` | `INT NOT NULL DEFAULT 0` | `int` | Millas disponibles para usar |
| `joined_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Fecha de adhesión — inmutable |

**UNIQUE:** `(passenger_id, loyalty_program_id)` — un pasajero, una cuenta por programa.  
**CHECK:** `available_miles <= total_miles` — espejado en dominio.  
**[IR-3]:** FK compuesta garantiza que el tier pertenece al mismo programa de la cuenta.  
`passenger_id`, `loyalty_program_id` y `joined_at` son inmutables.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/LoyaltyAccount/Domain/valueObject/LoyaltyAccountId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;

public sealed class LoyaltyAccountId
{
    public int Value { get; }

    public LoyaltyAccountId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("LoyaltyAccountId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is LoyaltyAccountId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Domain/aggregate/LoyaltyAccountAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;

/// <summary>
/// Cuenta de un pasajero en un programa de fidelización.
/// SQL: loyalty_account. [NC-4] id renombrado a loyalty_account_id.
///
/// [IR-3] FK compuesta (loyalty_program_id, loyalty_tier_id) garantiza que
///        el tier asignado pertenece al mismo programa de la cuenta.
/// UNIQUE: (passenger_id, loyalty_program_id) — un pasajero, una cuenta por programa.
/// CHECK:  available_miles <= total_miles — espejado en dominio.
///
/// Ciclo de vida:
///   - AddMiles()    → acumula millas (total + available).
///   - RedeemMiles() → resta millas disponibles (available solo).
///   - UpgradeTier() → promueve al pasajero a un tier superior.
///   - passenger_id, loyalty_program_id y joined_at son inmutables.
/// </summary>
public sealed class LoyaltyAccountAggregate
{
    public LoyaltyAccountId Id               { get; private set; }
    public int              PassengerId      { get; private set; }
    public int              LoyaltyProgramId { get; private set; }
    public int              LoyaltyTierId    { get; private set; }
    public int              TotalMiles       { get; private set; }
    public int              AvailableMiles   { get; private set; }
    public DateTime         JoinedAt         { get; private set; }

    private LoyaltyAccountAggregate()
    {
        Id = null!;
    }

    public LoyaltyAccountAggregate(
        LoyaltyAccountId id,
        int              passengerId,
        int              loyaltyProgramId,
        int              loyaltyTierId,
        int              totalMiles,
        int              availableMiles,
        DateTime         joinedAt)
    {
        if (passengerId <= 0)
            throw new ArgumentException(
                "PassengerId must be a positive integer.", nameof(passengerId));

        if (loyaltyProgramId <= 0)
            throw new ArgumentException(
                "LoyaltyProgramId must be a positive integer.", nameof(loyaltyProgramId));

        if (loyaltyTierId <= 0)
            throw new ArgumentException(
                "LoyaltyTierId must be a positive integer.", nameof(loyaltyTierId));

        ValidateMiles(totalMiles, availableMiles);

        Id               = id;
        PassengerId      = passengerId;
        LoyaltyProgramId = loyaltyProgramId;
        LoyaltyTierId    = loyaltyTierId;
        TotalMiles       = totalMiles;
        AvailableMiles   = availableMiles;
        JoinedAt         = joinedAt;
    }

    /// <summary>
    /// Acumula millas: incrementa total y available.
    /// </summary>
    public void AddMiles(int miles)
    {
        if (miles <= 0)
            throw new ArgumentException("Miles to add must be positive.", nameof(miles));

        TotalMiles     += miles;
        AvailableMiles += miles;
    }

    /// <summary>
    /// Redime millas: decrementa solo available (las millas históricas se preservan).
    /// </summary>
    public void RedeemMiles(int miles)
    {
        if (miles <= 0)
            throw new ArgumentException("Miles to redeem must be positive.", nameof(miles));

        if (miles > AvailableMiles)
            throw new InvalidOperationException(
                $"Insufficient available miles. Available: {AvailableMiles}, requested: {miles}.");

        AvailableMiles -= miles;
    }

    /// <summary>
    /// Promueve al pasajero a un nuevo tier.
    /// La integridad de que el tier pertenezca al mismo programa se garantiza
    /// mediante la FK compuesta [IR-3] en la base de datos.
    /// </summary>
    public void UpgradeTier(int loyaltyTierId)
    {
        if (loyaltyTierId <= 0)
            throw new ArgumentException(
                "LoyaltyTierId must be a positive integer.", nameof(loyaltyTierId));

        LoyaltyTierId = loyaltyTierId;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateMiles(int totalMiles, int availableMiles)
    {
        if (totalMiles < 0)
            throw new ArgumentException("TotalMiles must be >= 0.", nameof(totalMiles));

        if (availableMiles < 0)
            throw new ArgumentException("AvailableMiles must be >= 0.", nameof(availableMiles));

        if (availableMiles > totalMiles)
            throw new ArgumentException(
                "AvailableMiles cannot exceed TotalMiles. [chk_la_miles]");
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Domain/Repositories/ILoyaltyAccountRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccountAggregate?>             GetByIdAsync(LoyaltyAccountId id,                                CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyAccountAggregate>> GetAllAsync(                                                      CancellationToken cancellationToken = default);
    Task<LoyaltyAccountAggregate?>             GetByPassengerAndProgramAsync(int passengerId, int programId,    CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyAccountAggregate>> GetByPassengerAsync(int passengerId,                             CancellationToken cancellationToken = default);
    Task                                       AddAsync(LoyaltyAccountAggregate loyaltyAccount,                 CancellationToken cancellationToken = default);
    Task                                       UpdateAsync(LoyaltyAccountAggregate loyaltyAccount,              CancellationToken cancellationToken = default);
    Task                                       DeleteAsync(LoyaltyAccountId id,                                 CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/LoyaltyAccount/Application/Interfaces/ILoyaltyAccountService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.Interfaces;

public interface ILoyaltyAccountService
{
    Task<LoyaltyAccountDto?>             GetByIdAsync(int id,                                                                     CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyAccountDto>> GetAllAsync(                                                                             CancellationToken cancellationToken = default);
    Task<LoyaltyAccountDto?>             GetByPassengerAndProgramAsync(int passengerId, int programId,                            CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyAccountDto>> GetByPassengerAsync(int passengerId,                                                     CancellationToken cancellationToken = default);
    Task<LoyaltyAccountDto>              CreateAsync(int passengerId, int loyaltyProgramId, int loyaltyTierId,                    CancellationToken cancellationToken = default);
    Task                                 AddMilesAsync(int id, int miles,                                                         CancellationToken cancellationToken = default);
    Task                                 RedeemMilesAsync(int id, int miles,                                                      CancellationToken cancellationToken = default);
    Task                                 UpgradeTierAsync(int id, int loyaltyTierId,                                              CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(int id,                                                                      CancellationToken cancellationToken = default);
}

public sealed record LoyaltyAccountDto(
    int      Id,
    int      PassengerId,
    int      LoyaltyProgramId,
    int      LoyaltyTierId,
    int      TotalMiles,
    int      AvailableMiles,
    DateTime JoinedAt);
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/CreateLoyaltyAccountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateLoyaltyAccountUseCase
{
    private readonly ILoyaltyAccountRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public CreateLoyaltyAccountUseCase(ILoyaltyAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyAccountAggregate> ExecuteAsync(
        int               passengerId,
        int               loyaltyProgramId,
        int               loyaltyTierId,
        CancellationToken cancellationToken = default)
    {
        // LoyaltyAccountId(1) es placeholder; EF Core asigna el Id real al insertar.
        var account = new LoyaltyAccountAggregate(
            new LoyaltyAccountId(1),
            passengerId,
            loyaltyProgramId,
            loyaltyTierId,
            totalMiles:     0,
            availableMiles: 0,
            DateTime.UtcNow);

        await _repository.AddAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return account;
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/DeleteLoyaltyAccountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteLoyaltyAccountUseCase
{
    private readonly ILoyaltyAccountRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public DeleteLoyaltyAccountUseCase(ILoyaltyAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new LoyaltyAccountId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/GetAllLoyaltyAccountsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;

public sealed class GetAllLoyaltyAccountsUseCase
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetAllLoyaltyAccountsUseCase(ILoyaltyAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyAccountAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/GetLoyaltyAccountByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;

public sealed class GetLoyaltyAccountByIdUseCase
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetLoyaltyAccountByIdUseCase(ILoyaltyAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<LoyaltyAccountAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new LoyaltyAccountId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/AddMilesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class AddMilesUseCase
{
    private readonly ILoyaltyAccountRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public AddMilesUseCase(ILoyaltyAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, int miles, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(new LoyaltyAccountId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"LoyaltyAccount with id {id} was not found.");

        account.AddMiles(miles);
        await _repository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/RedeemMilesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class RedeemMilesUseCase
{
    private readonly ILoyaltyAccountRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public RedeemMilesUseCase(ILoyaltyAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, int miles, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(new LoyaltyAccountId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"LoyaltyAccount with id {id} was not found.");

        account.RedeemMiles(miles);
        await _repository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/UpgradeTierUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpgradeTierUseCase
{
    private readonly ILoyaltyAccountRepository _repository;
    private readonly IUnitOfWork               _unitOfWork;

    public UpgradeTierUseCase(ILoyaltyAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, int loyaltyTierId, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByIdAsync(new LoyaltyAccountId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"LoyaltyAccount with id {id} was not found.");

        account.UpgradeTier(loyaltyTierId);
        await _repository.UpdateAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/UseCases/GetLoyaltyAccountsByPassengerUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;

public sealed class GetLoyaltyAccountsByPassengerUseCase
{
    private readonly ILoyaltyAccountRepository _repository;

    public GetLoyaltyAccountsByPassengerUseCase(ILoyaltyAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyAccountAggregate>> ExecuteAsync(
        int               passengerId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByPassengerAsync(passengerId, cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Application/Services/LoyaltyAccountService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;

public sealed class LoyaltyAccountService : ILoyaltyAccountService
{
    private readonly CreateLoyaltyAccountUseCase            _create;
    private readonly DeleteLoyaltyAccountUseCase            _delete;
    private readonly GetAllLoyaltyAccountsUseCase           _getAll;
    private readonly GetLoyaltyAccountByIdUseCase           _getById;
    private readonly AddMilesUseCase                        _addMiles;
    private readonly RedeemMilesUseCase                     _redeemMiles;
    private readonly UpgradeTierUseCase                     _upgradeTier;
    private readonly GetLoyaltyAccountsByPassengerUseCase   _getByPassenger;

    public LoyaltyAccountService(
        CreateLoyaltyAccountUseCase          create,
        DeleteLoyaltyAccountUseCase          delete,
        GetAllLoyaltyAccountsUseCase         getAll,
        GetLoyaltyAccountByIdUseCase         getById,
        AddMilesUseCase                      addMiles,
        RedeemMilesUseCase                   redeemMiles,
        UpgradeTierUseCase                   upgradeTier,
        GetLoyaltyAccountsByPassengerUseCase getByPassenger)
    {
        _create         = create;
        _delete         = delete;
        _getAll         = getAll;
        _getById        = getById;
        _addMiles       = addMiles;
        _redeemMiles    = redeemMiles;
        _upgradeTier    = upgradeTier;
        _getByPassenger = getByPassenger;
    }

    public async Task<LoyaltyAccountDto> CreateAsync(
        int               passengerId,
        int               loyaltyProgramId,
        int               loyaltyTierId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            passengerId, loyaltyProgramId, loyaltyTierId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<LoyaltyAccountDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<LoyaltyAccountDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task AddMilesAsync(int id, int miles, CancellationToken cancellationToken = default)
        => await _addMiles.ExecuteAsync(id, miles, cancellationToken);

    public async Task RedeemMilesAsync(int id, int miles, CancellationToken cancellationToken = default)
        => await _redeemMiles.ExecuteAsync(id, miles, cancellationToken);

    public async Task UpgradeTierAsync(int id, int loyaltyTierId, CancellationToken cancellationToken = default)
        => await _upgradeTier.ExecuteAsync(id, loyaltyTierId, cancellationToken);

    public async Task<LoyaltyAccountDto?> GetByPassengerAndProgramAsync(
        int               passengerId,
        int               programId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByPassenger.ExecuteAsync(passengerId, cancellationToken);
        var found = agg.FirstOrDefault(a => a.LoyaltyProgramId == programId);
        return found is null ? null : ToDto(found);
    }

    public async Task<IEnumerable<LoyaltyAccountDto>> GetByPassengerAsync(
        int               passengerId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByPassenger.ExecuteAsync(passengerId, cancellationToken);
        return list.Select(ToDto);
    }

    private static LoyaltyAccountDto ToDto(LoyaltyAccountAggregate agg)
        => new(
            agg.Id.Value,
            agg.PassengerId,
            agg.LoyaltyProgramId,
            agg.LoyaltyTierId,
            agg.TotalMiles,
            agg.AvailableMiles,
            agg.JoinedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/LoyaltyAccount/Infrastructure/entity/LoyaltyAccountEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Infrastructure.Entity;

public sealed class LoyaltyAccountEntity
{
    public int      Id               { get; set; }
    public int      PassengerId      { get; set; }
    public int      LoyaltyProgramId { get; set; }
    public int      LoyaltyTierId    { get; set; }
    public int      TotalMiles       { get; set; }
    public int      AvailableMiles   { get; set; }
    public DateTime JoinedAt         { get; set; }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Infrastructure/entity/LoyaltyAccountEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class LoyaltyAccountEntityConfiguration : IEntityTypeConfiguration<LoyaltyAccountEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccountEntity> builder)
    {
        builder.ToTable("loyalty_account");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("loyalty_account_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.PassengerId)
               .HasColumnName("passenger_id")
               .IsRequired();

        builder.Property(e => e.LoyaltyProgramId)
               .HasColumnName("loyalty_program_id")
               .IsRequired();

        // UNIQUE (passenger_id, loyalty_program_id) — espejo de uq_la
        builder.HasIndex(e => new { e.PassengerId, e.LoyaltyProgramId })
               .IsUnique()
               .HasDatabaseName("uq_la");

        builder.Property(e => e.LoyaltyTierId)
               .HasColumnName("loyalty_tier_id")
               .IsRequired();

        // Nota: La FK compuesta (loyalty_program_id, loyalty_tier_id) → loyalty_tier
        // está definida en el DDL SQL [IR-3]. EF Core con Pomelo la respetará
        // en tiempo de ejecución vía las constraints de la base de datos.
        // No se modela como navegación para mantener la arquitectura hexagonal pura.

        builder.Property(e => e.TotalMiles)
               .HasColumnName("total_miles")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.AvailableMiles)
               .HasColumnName("available_miles")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.JoinedAt)
               .HasColumnName("joined_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

---

### RUTA: `src/Modules/LoyaltyAccount/Infrastructure/repository/LoyaltyAccountRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class LoyaltyAccountRepository : ILoyaltyAccountRepository
{
    private readonly AppDbContext _context;

    public LoyaltyAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    private static LoyaltyAccountAggregate ToDomain(LoyaltyAccountEntity entity)
        => new(
            new LoyaltyAccountId(entity.Id),
            entity.PassengerId,
            entity.LoyaltyProgramId,
            entity.LoyaltyTierId,
            entity.TotalMiles,
            entity.AvailableMiles,
            entity.JoinedAt);

    public async Task<LoyaltyAccountAggregate?> GetByIdAsync(
        LoyaltyAccountId  id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<LoyaltyAccountAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyAccounts
            .AsNoTracking()
            .OrderBy(e => e.PassengerId)
            .ThenBy(e => e.LoyaltyProgramId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<LoyaltyAccountAggregate?> GetByPassengerAndProgramAsync(
        int               passengerId,
        int               programId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.PassengerId == passengerId && e.LoyaltyProgramId == programId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<LoyaltyAccountAggregate>> GetByPassengerAsync(
        int               passengerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyAccounts
            .AsNoTracking()
            .Where(e => e.PassengerId == passengerId)
            .OrderBy(e => e.LoyaltyProgramId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        LoyaltyAccountAggregate loyaltyAccount,
        CancellationToken       cancellationToken = default)
    {
        var entity = new LoyaltyAccountEntity
        {
            PassengerId      = loyaltyAccount.PassengerId,
            LoyaltyProgramId = loyaltyAccount.LoyaltyProgramId,
            LoyaltyTierId    = loyaltyAccount.LoyaltyTierId,
            TotalMiles       = loyaltyAccount.TotalMiles,
            AvailableMiles   = loyaltyAccount.AvailableMiles,
            JoinedAt         = loyaltyAccount.JoinedAt
        };
        await _context.LoyaltyAccounts.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        LoyaltyAccountAggregate loyaltyAccount,
        CancellationToken       cancellationToken = default)
    {
        var entity = await _context.LoyaltyAccounts
            .FirstOrDefaultAsync(e => e.Id == loyaltyAccount.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyAccountEntity with id {loyaltyAccount.Id.Value} not found.");

        // PassengerId, LoyaltyProgramId y JoinedAt son inmutables.
        entity.LoyaltyTierId  = loyaltyAccount.LoyaltyTierId;
        entity.TotalMiles     = loyaltyAccount.TotalMiles;
        entity.AvailableMiles = loyaltyAccount.AvailableMiles;

        _context.LoyaltyAccounts.Update(entity);
    }

    public async Task DeleteAsync(
        LoyaltyAccountId  id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.LoyaltyAccounts
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyAccountEntity with id {id.Value} not found.");

        _context.LoyaltyAccounts.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/LoyaltyAccount/UI/LoyaltyAccountConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyAccount.Application.Interfaces;

public sealed class LoyaltyAccountConsoleUI
{
    private readonly ILoyaltyAccountService _service;

    public LoyaltyAccountConsoleUI(ILoyaltyAccountService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== LOYALTY ACCOUNT MODULE ==========");
            Console.WriteLine("1. List all accounts");
            Console.WriteLine("2. Get account by ID");
            Console.WriteLine("3. Get accounts by passenger");
            Console.WriteLine("4. Enroll passenger");
            Console.WriteLine("5. Add miles");
            Console.WriteLine("6. Redeem miles");
            Console.WriteLine("7. Upgrade tier");
            Console.WriteLine("8. Delete account");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByPassengerAsync();  break;
                case "4": await EnrollAsync();           break;
                case "5": await AddMilesAsync();         break;
                case "6": await RedeemMilesAsync();      break;
                case "7": await UpgradeTierAsync();      break;
                case "8": await DeleteAsync();           break;
                case "0": running = false;               break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private async Task ListAllAsync()
    {
        var accounts = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Loyalty Accounts ---");
        foreach (var a in accounts) PrintAccount(a);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var a = await _service.GetByIdAsync(id);
        if (a is null) Console.WriteLine($"Account with ID {id} not found.");
        else           PrintAccount(a);
    }

    private async Task ListByPassengerAsync()
    {
        Console.Write("Enter Passenger ID: ");
        if (!int.TryParse(Console.ReadLine(), out int passengerId))
        { Console.WriteLine("Invalid ID."); return; }

        var accounts = await _service.GetByPassengerAsync(passengerId);
        Console.WriteLine($"\n--- Loyalty Accounts for Passenger {passengerId} ---");
        foreach (var a in accounts) PrintAccount(a);
    }

    private async Task EnrollAsync()
    {
        Console.Write("Passenger ID: ");
        if (!int.TryParse(Console.ReadLine(), out int passengerId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Loyalty Program ID: ");
        if (!int.TryParse(Console.ReadLine(), out int programId)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Initial Tier ID: ");
        if (!int.TryParse(Console.ReadLine(), out int tierId)) { Console.WriteLine("Invalid."); return; }

        try
        {
            var created = await _service.CreateAsync(passengerId, programId, tierId);
            Console.WriteLine($"Enrolled: [{created.Id}]");
            PrintAccount(created);
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task AddMilesAsync()
    {
        Console.Write("Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Miles to add (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int miles) || miles <= 0)
        { Console.WriteLine("Invalid miles."); return; }

        try
        {
            await _service.AddMilesAsync(id, miles);
            Console.WriteLine($"{miles:N0} miles added successfully.");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task RedeemMilesAsync()
    {
        Console.Write("Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("Miles to redeem (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int miles) || miles <= 0)
        { Console.WriteLine("Invalid miles."); return; }

        try
        {
            await _service.RedeemMilesAsync(id, miles);
            Console.WriteLine($"{miles:N0} miles redeemed successfully.");
        }
        catch (InvalidOperationException ex) { Console.WriteLine($"Business rule error: {ex.Message}"); }
        catch (ArgumentException ex)         { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task UpgradeTierAsync()
    {
        Console.Write("Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        Console.Write("New Tier ID: ");
        if (!int.TryParse(Console.ReadLine(), out int tierId)) { Console.WriteLine("Invalid."); return; }

        await _service.UpgradeTierAsync(id, tierId);
        Console.WriteLine("Tier upgraded successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Account ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Account deleted successfully.");
    }

    private static void PrintAccount(LoyaltyAccountDto a)
        => Console.WriteLine(
            $"  [{a.Id}] Passenger:{a.PassengerId} | Program:{a.LoyaltyProgramId} | " +
            $"Tier:{a.LoyaltyTierId} | Total:{a.TotalMiles:N0} | " +
            $"Available:{a.AvailableMiles:N0} | Joined:{a.JoinedAt:yyyy-MM-dd}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento)_

```csharp
// ── LoyaltyAccount Module ─────────────────────────────────────────────────────
builder.Services.AddScoped<ILoyaltyAccountRepository, LoyaltyAccountRepository>();
builder.Services.AddScoped<CreateLoyaltyAccountUseCase>();
builder.Services.AddScoped<DeleteLoyaltyAccountUseCase>();
builder.Services.AddScoped<GetAllLoyaltyAccountsUseCase>();
builder.Services.AddScoped<GetLoyaltyAccountByIdUseCase>();
builder.Services.AddScoped<AddMilesUseCase>();
builder.Services.AddScoped<RedeemMilesUseCase>();
builder.Services.AddScoped<UpgradeTierUseCase>();
builder.Services.AddScoped<GetLoyaltyAccountsByPassengerUseCase>();
builder.Services.AddScoped<ILoyaltyAccountService, LoyaltyAccountService>();
builder.Services.AddScoped<LoyaltyAccountConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/LoyaltyAccount/
├── Application/
│   ├── Interfaces/
│   │   └── ILoyaltyAccountService.cs
│   ├── Services/
│   │   └── LoyaltyAccountService.cs
│   └── UseCases/
│       ├── AddMilesUseCase.cs
│       ├── CreateLoyaltyAccountUseCase.cs
│       ├── DeleteLoyaltyAccountUseCase.cs
│       ├── GetAllLoyaltyAccountsUseCase.cs
│       ├── GetLoyaltyAccountByIdUseCase.cs
│       ├── GetLoyaltyAccountsByPassengerUseCase.cs
│       ├── RedeemMilesUseCase.cs
│       └── UpgradeTierUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── LoyaltyAccountAggregate.cs
│   ├── Repositories/
│   │   └── ILoyaltyAccountRepository.cs
│   └── valueObject/
│       └── LoyaltyAccountId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── LoyaltyAccountEntity.cs
│   │   └── LoyaltyAccountEntityConfiguration.cs
│   └── repository/
│       └── LoyaltyAccountRepository.cs
└── UI/
    └── LoyaltyAccountConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo LoyaltyAccount_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
