# Módulo: PassengerDiscount
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount`  
**Raíz de archivos:** `src/Modules/PassengerDiscount/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `passenger_discount_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-3] |
| `reservation_detail_id` | `INT NOT NULL FK` | `int` | FK → `reservation_detail` |
| `discount_type_id` | `INT NOT NULL FK` | `int` | FK → `discount_type` |
| `amount_applied` | `DECIMAL(12,2) NOT NULL` | `decimal` | CHECK `>= 0` [IR-5] — validado en dominio |

**UNIQUE:** `(reservation_detail_id, discount_type_id)` — un tipo de descuento solo se aplica una vez por línea de reserva.  
**CHECK [IR-5]:** `amount_applied >= 0` — espejado en el constructor del agregado.  
Sin `created_at` ni `updated_at` en el DDL.  
La única actualización semánticamente válida es modificar `amount_applied`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/PassengerDiscount/Domain/valueObject/PassengerDiscountId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;

public sealed class PassengerDiscountId
{
    public int Value { get; }

    public PassengerDiscountId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("PassengerDiscountId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is PassengerDiscountId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Domain/aggregate/PassengerDiscountAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;

/// <summary>
/// Descuento aplicado a un pasajero en una línea de reserva concreta.
/// SQL: passenger_discount. [NC-3] id renombrado a passenger_discount_id.
///
/// Invariantes:
///   - amount_applied >= 0 [IR-5] — espejo del CHECK SQL.
///   - UNIQUE (reservation_detail_id, discount_type_id): un tipo de descuento
///     solo puede aplicarse una vez por línea de reserva.
///
/// La única modificación válida es actualizar el monto aplicado (AdjustAmount).
/// reservation_detail_id y discount_type_id forman la clave de negocio — inmutables.
/// </summary>
public sealed class PassengerDiscountAggregate
{
    public PassengerDiscountId Id                  { get; private set; }
    public int                 ReservationDetailId { get; private set; }
    public int                 DiscountTypeId      { get; private set; }
    public decimal             AmountApplied       { get; private set; }

    private PassengerDiscountAggregate()
    {
        Id = null!;
    }

    public PassengerDiscountAggregate(
        PassengerDiscountId id,
        int                 reservationDetailId,
        int                 discountTypeId,
        decimal             amountApplied)
    {
        if (reservationDetailId <= 0)
            throw new ArgumentException(
                "ReservationDetailId must be a positive integer.", nameof(reservationDetailId));

        if (discountTypeId <= 0)
            throw new ArgumentException(
                "DiscountTypeId must be a positive integer.", nameof(discountTypeId));

        ValidateAmount(amountApplied);

        Id                  = id;
        ReservationDetailId = reservationDetailId;
        DiscountTypeId      = discountTypeId;
        AmountApplied       = amountApplied;
    }

    /// <summary>
    /// Ajusta el monto del descuento aplicado.
    /// reservation_detail_id y discount_type_id son la clave de negocio — inmutables.
    /// </summary>
    public void AdjustAmount(decimal newAmount)
    {
        ValidateAmount(newAmount);
        AmountApplied = newAmount;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException(
                "AmountApplied must be >= 0. [IR-5]", nameof(amount));
    }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Domain/Repositories/IPassengerDiscountRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;

public interface IPassengerDiscountRepository
{
    Task<PassengerDiscountAggregate?>             GetByIdAsync(PassengerDiscountId id,                      CancellationToken cancellationToken = default);
    Task<IEnumerable<PassengerDiscountAggregate>> GetAllAsync(                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<PassengerDiscountAggregate>> GetByReservationDetailAsync(int reservationDetailId,       CancellationToken cancellationToken = default);
    Task                                          AddAsync(PassengerDiscountAggregate passengerDiscount,     CancellationToken cancellationToken = default);
    Task                                          UpdateAsync(PassengerDiscountAggregate passengerDiscount,  CancellationToken cancellationToken = default);
    Task                                          DeleteAsync(PassengerDiscountId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/PassengerDiscount/Application/Interfaces/IPassengerDiscountService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.Interfaces;

public interface IPassengerDiscountService
{
    Task<PassengerDiscountDto?>             GetByIdAsync(int id,                                                                   CancellationToken cancellationToken = default);
    Task<IEnumerable<PassengerDiscountDto>> GetAllAsync(                                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<PassengerDiscountDto>> GetByReservationDetailAsync(int reservationDetailId,                                   CancellationToken cancellationToken = default);
    Task<PassengerDiscountDto>              CreateAsync(int reservationDetailId, int discountTypeId, decimal amountApplied,        CancellationToken cancellationToken = default);
    Task                                    AdjustAmountAsync(int id, decimal newAmount,                                           CancellationToken cancellationToken = default);
    Task                                    DeleteAsync(int id,                                                                    CancellationToken cancellationToken = default);
}

public sealed record PassengerDiscountDto(
    int     Id,
    int     ReservationDetailId,
    int     DiscountTypeId,
    decimal AmountApplied);
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/CreatePassengerDiscountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreatePassengerDiscountUseCase
{
    private readonly IPassengerDiscountRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public CreatePassengerDiscountUseCase(IPassengerDiscountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PassengerDiscountAggregate> ExecuteAsync(
        int               reservationDetailId,
        int               discountTypeId,
        decimal           amountApplied,
        CancellationToken cancellationToken = default)
    {
        // PassengerDiscountId(1) es placeholder; EF Core asigna el Id real al insertar.
        var discount = new PassengerDiscountAggregate(
            new PassengerDiscountId(1),
            reservationDetailId,
            discountTypeId,
            amountApplied);

        await _repository.AddAsync(discount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return discount;
    }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/DeletePassengerDiscountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeletePassengerDiscountUseCase
{
    private readonly IPassengerDiscountRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public DeletePassengerDiscountUseCase(IPassengerDiscountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new PassengerDiscountId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/GetAllPassengerDiscountsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;

public sealed class GetAllPassengerDiscountsUseCase
{
    private readonly IPassengerDiscountRepository _repository;

    public GetAllPassengerDiscountsUseCase(IPassengerDiscountRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PassengerDiscountAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/GetPassengerDiscountByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;

public sealed class GetPassengerDiscountByIdUseCase
{
    private readonly IPassengerDiscountRepository _repository;

    public GetPassengerDiscountByIdUseCase(IPassengerDiscountRepository repository)
    {
        _repository = repository;
    }

    public async Task<PassengerDiscountAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new PassengerDiscountId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/UpdatePassengerDiscountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Ajusta el monto del descuento aplicado a un pasajero en una línea de reserva.
/// reservation_detail_id y discount_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class UpdatePassengerDiscountUseCase
{
    private readonly IPassengerDiscountRepository _repository;
    private readonly IUnitOfWork                  _unitOfWork;

    public UpdatePassengerDiscountUseCase(IPassengerDiscountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        decimal           newAmount,
        CancellationToken cancellationToken = default)
    {
        var discount = await _repository.GetByIdAsync(new PassengerDiscountId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"PassengerDiscount with id {id} was not found.");

        discount.AdjustAmount(newAmount);
        await _repository.UpdateAsync(discount, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/UseCases/GetPassengerDiscountsByDetailUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;

/// <summary>
/// Obtiene todos los descuentos aplicados a una línea de reserva concreta.
/// Caso de uso clave para calcular el precio neto de un pasajero.
/// </summary>
public sealed class GetPassengerDiscountsByDetailUseCase
{
    private readonly IPassengerDiscountRepository _repository;

    public GetPassengerDiscountsByDetailUseCase(IPassengerDiscountRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PassengerDiscountAggregate>> ExecuteAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByReservationDetailAsync(reservationDetailId, cancellationToken);
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Application/Services/PassengerDiscountService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;

public sealed class PassengerDiscountService : IPassengerDiscountService
{
    private readonly CreatePassengerDiscountUseCase           _create;
    private readonly DeletePassengerDiscountUseCase           _delete;
    private readonly GetAllPassengerDiscountsUseCase          _getAll;
    private readonly GetPassengerDiscountByIdUseCase          _getById;
    private readonly UpdatePassengerDiscountUseCase           _update;
    private readonly GetPassengerDiscountsByDetailUseCase     _getByDetail;

    public PassengerDiscountService(
        CreatePassengerDiscountUseCase       create,
        DeletePassengerDiscountUseCase       delete,
        GetAllPassengerDiscountsUseCase      getAll,
        GetPassengerDiscountByIdUseCase      getById,
        UpdatePassengerDiscountUseCase       update,
        GetPassengerDiscountsByDetailUseCase getByDetail)
    {
        _create      = create;
        _delete      = delete;
        _getAll      = getAll;
        _getById     = getById;
        _update      = update;
        _getByDetail = getByDetail;
    }

    public async Task<PassengerDiscountDto> CreateAsync(
        int               reservationDetailId,
        int               discountTypeId,
        decimal           amountApplied,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            reservationDetailId, discountTypeId, amountApplied, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<PassengerDiscountDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<PassengerDiscountDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task AdjustAmountAsync(
        int               id,
        decimal           newAmount,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, newAmount, cancellationToken);

    public async Task<IEnumerable<PassengerDiscountDto>> GetByReservationDetailAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByDetail.ExecuteAsync(reservationDetailId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static PassengerDiscountDto ToDto(PassengerDiscountAggregate agg)
        => new(agg.Id.Value, agg.ReservationDetailId, agg.DiscountTypeId, agg.AmountApplied);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/PassengerDiscount/Infrastructure/entity/PassengerDiscountEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Infrastructure.Entity;

public sealed class PassengerDiscountEntity
{
    public int     Id                  { get; set; }
    public int     ReservationDetailId { get; set; }
    public int     DiscountTypeId      { get; set; }
    public decimal AmountApplied       { get; set; }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Infrastructure/entity/PassengerDiscountEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PassengerDiscountEntityConfiguration : IEntityTypeConfiguration<PassengerDiscountEntity>
{
    public void Configure(EntityTypeBuilder<PassengerDiscountEntity> builder)
    {
        builder.ToTable("passenger_discount");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("passenger_discount_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.ReservationDetailId)
               .HasColumnName("reservation_detail_id")
               .IsRequired();

        builder.Property(e => e.DiscountTypeId)
               .HasColumnName("discount_type_id")
               .IsRequired();

        builder.Property(e => e.AmountApplied)
               .HasColumnName("amount_applied")
               .IsRequired()
               .HasColumnType("decimal(12,2)");

        // UNIQUE (reservation_detail_id, discount_type_id) — espejo de uq_pd
        builder.HasIndex(e => new { e.ReservationDetailId, e.DiscountTypeId })
               .IsUnique()
               .HasDatabaseName("uq_pd");
    }
}
```

---

### RUTA: `src/Modules/PassengerDiscount/Infrastructure/repository/PassengerDiscountRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class PassengerDiscountRepository : IPassengerDiscountRepository
{
    private readonly AppDbContext _context;

    public PassengerDiscountRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static PassengerDiscountAggregate ToDomain(PassengerDiscountEntity entity)
        => new(
            new PassengerDiscountId(entity.Id),
            entity.ReservationDetailId,
            entity.DiscountTypeId,
            entity.AmountApplied);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<PassengerDiscountAggregate?> GetByIdAsync(
        PassengerDiscountId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.PassengerDiscounts
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<PassengerDiscountAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PassengerDiscounts
            .AsNoTracking()
            .OrderBy(e => e.ReservationDetailId)
            .ThenBy(e => e.DiscountTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<PassengerDiscountAggregate>> GetByReservationDetailAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PassengerDiscounts
            .AsNoTracking()
            .Where(e => e.ReservationDetailId == reservationDetailId)
            .OrderBy(e => e.DiscountTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        PassengerDiscountAggregate passengerDiscount,
        CancellationToken          cancellationToken = default)
    {
        var entity = new PassengerDiscountEntity
        {
            ReservationDetailId = passengerDiscount.ReservationDetailId,
            DiscountTypeId      = passengerDiscount.DiscountTypeId,
            AmountApplied       = passengerDiscount.AmountApplied
        };
        await _context.PassengerDiscounts.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        PassengerDiscountAggregate passengerDiscount,
        CancellationToken          cancellationToken = default)
    {
        var entity = await _context.PassengerDiscounts
            .FirstOrDefaultAsync(e => e.Id == passengerDiscount.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PassengerDiscountEntity with id {passengerDiscount.Id.Value} not found.");

        // Solo AmountApplied es mutable.
        // ReservationDetailId y DiscountTypeId forman la clave de negocio.
        entity.AmountApplied = passengerDiscount.AmountApplied;

        _context.PassengerDiscounts.Update(entity);
    }

    public async Task DeleteAsync(
        PassengerDiscountId id,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.PassengerDiscounts
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"PassengerDiscountEntity with id {id.Value} not found.");

        _context.PassengerDiscounts.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/PassengerDiscount/UI/PassengerDiscountConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.PassengerDiscount.Application.Interfaces;

public sealed class PassengerDiscountConsoleUI
{
    private readonly IPassengerDiscountService _service;

    public PassengerDiscountConsoleUI(IPassengerDiscountService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== PASSENGER DISCOUNT MODULE ==========");
            Console.WriteLine("1. List all discounts");
            Console.WriteLine("2. Get discount by ID");
            Console.WriteLine("3. List discounts by reservation detail");
            Console.WriteLine("4. Apply discount");
            Console.WriteLine("5. Adjust amount");
            Console.WriteLine("6. Remove discount");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();          break;
                case "2": await GetByIdAsync();          break;
                case "3": await ListByDetailAsync();     break;
                case "4": await ApplyDiscountAsync();    break;
                case "5": await AdjustAmountAsync();     break;
                case "6": await RemoveDiscountAsync();   break;
                case "0": running = false;               break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var discounts = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Passenger Discounts ---");
        foreach (var d in discounts) PrintDiscount(d);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter discount ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var discount = await _service.GetByIdAsync(id);
        if (discount is null) Console.WriteLine($"Passenger discount with ID {id} not found.");
        else                  PrintDiscount(discount);
    }

    private async Task ListByDetailAsync()
    {
        Console.Write("Enter Reservation Detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int detailId))
        { Console.WriteLine("Invalid ID."); return; }

        var discounts = await _service.GetByReservationDetailAsync(detailId);
        Console.WriteLine($"\n--- Discounts for Reservation Detail {detailId} ---");

        decimal total = 0;
        foreach (var d in discounts)
        {
            PrintDiscount(d);
            total += d.AmountApplied;
        }
        Console.WriteLine($"  Total discounts applied: {total:F2}");
    }

    private async Task ApplyDiscountAsync()
    {
        Console.Write("Reservation Detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int detailId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Discount Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int discountTypeId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Amount applied (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal amount))
        { Console.WriteLine("Invalid amount."); return; }

        try
        {
            var created = await _service.CreateAsync(detailId, discountTypeId, amount);
            Console.WriteLine(
                $"Discount applied: [{created.Id}] Detail {created.ReservationDetailId} | " +
                $"Type {created.DiscountTypeId} | Amount: {created.AmountApplied:F2}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task AdjustAmountAsync()
    {
        Console.Write("Discount ID to adjust: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New amount (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal newAmount))
        { Console.WriteLine("Invalid amount."); return; }

        try
        {
            await _service.AdjustAmountAsync(id, newAmount);
            Console.WriteLine($"Amount adjusted to {newAmount:F2}.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task RemoveDiscountAsync()
    {
        Console.Write("Discount ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Discount removed successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintDiscount(PassengerDiscountDto d)
        => Console.WriteLine(
            $"  [{d.Id}] Detail: {d.ReservationDetailId} | " +
            $"Type: {d.DiscountTypeId} | Amount: {d.AmountApplied:F2}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── PassengerDiscount Module ──────────────────────────────────────────────────
builder.Services.AddScoped<IPassengerDiscountRepository, PassengerDiscountRepository>();
builder.Services.AddScoped<CreatePassengerDiscountUseCase>();
builder.Services.AddScoped<DeletePassengerDiscountUseCase>();
builder.Services.AddScoped<GetAllPassengerDiscountsUseCase>();
builder.Services.AddScoped<GetPassengerDiscountByIdUseCase>();
builder.Services.AddScoped<UpdatePassengerDiscountUseCase>();
builder.Services.AddScoped<GetPassengerDiscountsByDetailUseCase>();
builder.Services.AddScoped<IPassengerDiscountService, PassengerDiscountService>();
builder.Services.AddScoped<PassengerDiscountConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/PassengerDiscount/
├── Application/
│   ├── Interfaces/
│   │   └── IPassengerDiscountService.cs
│   ├── Services/
│   │   └── PassengerDiscountService.cs
│   └── UseCases/
│       ├── CreatePassengerDiscountUseCase.cs
│       ├── DeletePassengerDiscountUseCase.cs
│       ├── GetAllPassengerDiscountsUseCase.cs
│       ├── GetPassengerDiscountByIdUseCase.cs
│       ├── GetPassengerDiscountsByDetailUseCase.cs
│       └── UpdatePassengerDiscountUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── PassengerDiscountAggregate.cs
│   ├── Repositories/
│   │   └── IPassengerDiscountRepository.cs
│   └── valueObject/
│       └── PassengerDiscountId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── PassengerDiscountEntity.cs
│   │   └── PassengerDiscountEntityConfiguration.cs
│   └── repository/
│       └── PassengerDiscountRepository.cs
└── UI/
    └── PassengerDiscountConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo PassengerDiscount_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
