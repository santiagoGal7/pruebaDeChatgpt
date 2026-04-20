# Módulo: TicketBaggage
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage`  
**Raíz de archivos:** `src/Modules/TicketBaggage/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `ticket_baggage_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-2] |
| `ticket_id` | `INT NOT NULL FK` | `int` | FK → `ticket` |
| `baggage_type_id` | `INT NOT NULL FK` | `int` | FK → `baggage_type` |
| `quantity` | `INT NOT NULL DEFAULT 1` | `int` | CHECK `> 0` — espejado en dominio |
| `fee_charged` | `DECIMAL(10,2) NOT NULL DEFAULT 0` | `decimal` | Tarifa real cobrada en el momento de registrar |

**UNIQUE:** `(ticket_id, baggage_type_id)` — un tipo de equipaje solo se registra una vez por tiquete.  
**CHECK:** `quantity > 0` — espejado en constructor y `UpdateQuantity()`.  
Sin `created_at`, `updated_at` en el DDL.  
**4NF:** `(ticket_id, baggage_type_id) → quantity` — sin MVD independientes.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/TicketBaggage/Domain/valueObject/TicketBaggageId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;

public sealed class TicketBaggageId
{
    public int Value { get; }

    public TicketBaggageId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("TicketBaggageId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is TicketBaggageId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/TicketBaggage/Domain/aggregate/TicketBaggageAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;

/// <summary>
/// Equipaje adicional asociado a un tiquete.
/// SQL: ticket_baggage. [NC-2] id renombrado a ticket_baggage_id.
///
/// 4NF: (ticket_id, baggage_type_id) → quantity, fee_charged.
/// Sin MVD independientes — no viola 4NF.
/// UNIQUE: (ticket_id, baggage_type_id).
///
/// Invariantes:
///   - quantity > 0 (espejo de chk_tb_qty).
///   - fee_charged >= 0 (tarifa cobrada, no puede ser negativa).
///   - ticket_id y baggage_type_id son la clave de negocio — inmutables.
///
/// UpdateQuantity(): ajusta la cantidad de piezas de este tipo de equipaje.
/// fee_charged también puede actualizarse si el precio cambia al modificar la cantidad.
/// </summary>
public sealed class TicketBaggageAggregate
{
    public TicketBaggageId Id            { get; private set; }
    public int             TicketId      { get; private set; }
    public int             BaggageTypeId { get; private set; }
    public int             Quantity      { get; private set; }
    public decimal         FeeCharged    { get; private set; }

    private TicketBaggageAggregate()
    {
        Id = null!;
    }

    public TicketBaggageAggregate(
        TicketBaggageId id,
        int             ticketId,
        int             baggageTypeId,
        int             quantity,
        decimal         feeCharged)
    {
        if (ticketId <= 0)
            throw new ArgumentException(
                "TicketId must be a positive integer.", nameof(ticketId));

        if (baggageTypeId <= 0)
            throw new ArgumentException(
                "BaggageTypeId must be a positive integer.", nameof(baggageTypeId));

        ValidateQuantity(quantity);
        ValidateFeeCharged(feeCharged);

        Id            = id;
        TicketId      = ticketId;
        BaggageTypeId = baggageTypeId;
        Quantity      = quantity;
        FeeCharged    = feeCharged;
    }

    /// <summary>
    /// Actualiza la cantidad y tarifa cobrada para este tipo de equipaje.
    /// ticket_id y baggage_type_id son la clave de negocio — inmutables.
    /// </summary>
    public void UpdateQuantityAndFee(int quantity, decimal feeCharged)
    {
        ValidateQuantity(quantity);
        ValidateFeeCharged(feeCharged);

        Quantity   = quantity;
        FeeCharged = feeCharged;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than 0. [chk_tb_qty]", nameof(quantity));
    }

    private static void ValidateFeeCharged(decimal feeCharged)
    {
        if (feeCharged < 0)
            throw new ArgumentException(
                "FeeCharged must be >= 0.", nameof(feeCharged));
    }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Domain/Repositories/ITicketBaggageRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;

public interface ITicketBaggageRepository
{
    Task<TicketBaggageAggregate?>             GetByIdAsync(TicketBaggageId id,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketBaggageAggregate>> GetAllAsync(                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketBaggageAggregate>> GetByTicketAsync(int ticketId,                         CancellationToken cancellationToken = default);
    Task                                      AddAsync(TicketBaggageAggregate ticketBaggage,         CancellationToken cancellationToken = default);
    Task                                      UpdateAsync(TicketBaggageAggregate ticketBaggage,      CancellationToken cancellationToken = default);
    Task                                      DeleteAsync(TicketBaggageId id,                        CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/TicketBaggage/Application/Interfaces/ITicketBaggageService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.Interfaces;

public interface ITicketBaggageService
{
    Task<TicketBaggageDto?>             GetByIdAsync(int id,                                                              CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketBaggageDto>> GetAllAsync(                                                                      CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketBaggageDto>> GetByTicketAsync(int ticketId,                                                    CancellationToken cancellationToken = default);
    Task<TicketBaggageDto>              CreateAsync(int ticketId, int baggageTypeId, int quantity, decimal feeCharged,    CancellationToken cancellationToken = default);
    Task                                UpdateQuantityAndFeeAsync(int id, int quantity, decimal feeCharged,               CancellationToken cancellationToken = default);
    Task                                DeleteAsync(int id,                                                               CancellationToken cancellationToken = default);
}

public sealed record TicketBaggageDto(
    int     Id,
    int     TicketId,
    int     BaggageTypeId,
    int     Quantity,
    decimal FeeCharged);
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/CreateTicketBaggageUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateTicketBaggageUseCase
{
    private readonly ITicketBaggageRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreateTicketBaggageUseCase(ITicketBaggageRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketBaggageAggregate> ExecuteAsync(
        int               ticketId,
        int               baggageTypeId,
        int               quantity,
        decimal           feeCharged,
        CancellationToken cancellationToken = default)
    {
        // TicketBaggageId(1) es placeholder; EF Core asigna el Id real al insertar.
        var ticketBaggage = new TicketBaggageAggregate(
            new TicketBaggageId(1),
            ticketId, baggageTypeId, quantity, feeCharged);

        await _repository.AddAsync(ticketBaggage, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return ticketBaggage;
    }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/DeleteTicketBaggageUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteTicketBaggageUseCase
{
    private readonly ITicketBaggageRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeleteTicketBaggageUseCase(ITicketBaggageRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new TicketBaggageId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/GetAllTicketBaggagesUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;

public sealed class GetAllTicketBaggagesUseCase
{
    private readonly ITicketBaggageRepository _repository;

    public GetAllTicketBaggagesUseCase(ITicketBaggageRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TicketBaggageAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/GetTicketBaggageByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;

public sealed class GetTicketBaggageByIdUseCase
{
    private readonly ITicketBaggageRepository _repository;

    public GetTicketBaggageByIdUseCase(ITicketBaggageRepository repository)
    {
        _repository = repository;
    }

    public async Task<TicketBaggageAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new TicketBaggageId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/UpdateTicketBaggageUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Ajusta la cantidad de piezas y la tarifa cobrada.
/// ticket_id y baggage_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class UpdateTicketBaggageUseCase
{
    private readonly ITicketBaggageRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdateTicketBaggageUseCase(ITicketBaggageRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               quantity,
        decimal           feeCharged,
        CancellationToken cancellationToken = default)
    {
        var ticketBaggage = await _repository.GetByIdAsync(new TicketBaggageId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"TicketBaggage with id {id} was not found.");

        ticketBaggage.UpdateQuantityAndFee(quantity, feeCharged);
        await _repository.UpdateAsync(ticketBaggage, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/UseCases/GetTicketBaggagesByTicketUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;

/// <summary>
/// Obtiene todo el equipaje adicional registrado en un tiquete.
/// Caso de uso clave para mostrar el detalle completo del tiquete
/// y calcular el costo total de equipaje adicional.
/// </summary>
public sealed class GetTicketBaggagesByTicketUseCase
{
    private readonly ITicketBaggageRepository _repository;

    public GetTicketBaggagesByTicketUseCase(ITicketBaggageRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TicketBaggageAggregate>> ExecuteAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByTicketAsync(ticketId, cancellationToken);
}
```

---

### RUTA: `src/Modules/TicketBaggage/Application/Services/TicketBaggageService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;

public sealed class TicketBaggageService : ITicketBaggageService
{
    private readonly CreateTicketBaggageUseCase         _create;
    private readonly DeleteTicketBaggageUseCase         _delete;
    private readonly GetAllTicketBaggagesUseCase        _getAll;
    private readonly GetTicketBaggageByIdUseCase        _getById;
    private readonly UpdateTicketBaggageUseCase         _update;
    private readonly GetTicketBaggagesByTicketUseCase   _getByTicket;

    public TicketBaggageService(
        CreateTicketBaggageUseCase       create,
        DeleteTicketBaggageUseCase       delete,
        GetAllTicketBaggagesUseCase      getAll,
        GetTicketBaggageByIdUseCase      getById,
        UpdateTicketBaggageUseCase       update,
        GetTicketBaggagesByTicketUseCase getByTicket)
    {
        _create      = create;
        _delete      = delete;
        _getAll      = getAll;
        _getById     = getById;
        _update      = update;
        _getByTicket = getByTicket;
    }

    public async Task<TicketBaggageDto> CreateAsync(
        int               ticketId,
        int               baggageTypeId,
        int               quantity,
        decimal           feeCharged,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            ticketId, baggageTypeId, quantity, feeCharged, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<TicketBaggageDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<TicketBaggageDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateQuantityAndFeeAsync(
        int               id,
        int               quantity,
        decimal           feeCharged,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, quantity, feeCharged, cancellationToken);

    public async Task<IEnumerable<TicketBaggageDto>> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByTicket.ExecuteAsync(ticketId, cancellationToken);
        return list.Select(ToDto);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static TicketBaggageDto ToDto(TicketBaggageAggregate agg)
        => new(agg.Id.Value, agg.TicketId, agg.BaggageTypeId, agg.Quantity, agg.FeeCharged);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/TicketBaggage/Infrastructure/entity/TicketBaggageEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Infrastructure.Entity;

public sealed class TicketBaggageEntity
{
    public int     Id            { get; set; }
    public int     TicketId      { get; set; }
    public int     BaggageTypeId { get; set; }
    public int     Quantity      { get; set; }
    public decimal FeeCharged    { get; set; }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Infrastructure/entity/TicketBaggageEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TicketBaggageEntityConfiguration : IEntityTypeConfiguration<TicketBaggageEntity>
{
    public void Configure(EntityTypeBuilder<TicketBaggageEntity> builder)
    {
        builder.ToTable("ticket_baggage");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("ticket_baggage_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.TicketId)
               .HasColumnName("ticket_id")
               .IsRequired();

        builder.Property(e => e.BaggageTypeId)
               .HasColumnName("baggage_type_id")
               .IsRequired();

        // UNIQUE (ticket_id, baggage_type_id) — espejo de uq_tb
        builder.HasIndex(e => new { e.TicketId, e.BaggageTypeId })
               .IsUnique()
               .HasDatabaseName("uq_tb");

        builder.Property(e => e.Quantity)
               .HasColumnName("quantity")
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(e => e.FeeCharged)
               .HasColumnName("fee_charged")
               .IsRequired()
               .HasColumnType("decimal(10,2)")
               .HasDefaultValue(0m);
    }
}
```

---

### RUTA: `src/Modules/TicketBaggage/Infrastructure/repository/TicketBaggageRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class TicketBaggageRepository : ITicketBaggageRepository
{
    private readonly AppDbContext _context;

    public TicketBaggageRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static TicketBaggageAggregate ToDomain(TicketBaggageEntity entity)
        => new(
            new TicketBaggageId(entity.Id),
            entity.TicketId,
            entity.BaggageTypeId,
            entity.Quantity,
            entity.FeeCharged);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<TicketBaggageAggregate?> GetByIdAsync(
        TicketBaggageId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TicketBaggages
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<TicketBaggageAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TicketBaggages
            .AsNoTracking()
            .OrderBy(e => e.TicketId)
            .ThenBy(e => e.BaggageTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<TicketBaggageAggregate>> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.TicketBaggages
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderBy(e => e.BaggageTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        TicketBaggageAggregate ticketBaggage,
        CancellationToken      cancellationToken = default)
    {
        var entity = new TicketBaggageEntity
        {
            TicketId      = ticketBaggage.TicketId,
            BaggageTypeId = ticketBaggage.BaggageTypeId,
            Quantity      = ticketBaggage.Quantity,
            FeeCharged    = ticketBaggage.FeeCharged
        };
        await _context.TicketBaggages.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        TicketBaggageAggregate ticketBaggage,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.TicketBaggages
            .FirstOrDefaultAsync(e => e.Id == ticketBaggage.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketBaggageEntity with id {ticketBaggage.Id.Value} not found.");

        // Solo Quantity y FeeCharged son mutables.
        // TicketId y BaggageTypeId son la clave de negocio — inmutables.
        entity.Quantity   = ticketBaggage.Quantity;
        entity.FeeCharged = ticketBaggage.FeeCharged;

        _context.TicketBaggages.Update(entity);
    }

    public async Task DeleteAsync(
        TicketBaggageId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.TicketBaggages
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketBaggageEntity with id {id.Value} not found.");

        _context.TicketBaggages.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/TicketBaggage/UI/TicketBaggageConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.TicketBaggage.Application.Interfaces;

public sealed class TicketBaggageConsoleUI
{
    private readonly ITicketBaggageService _service;

    public TicketBaggageConsoleUI(ITicketBaggageService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== TICKET BAGGAGE MODULE ==========");
            Console.WriteLine("1. List all ticket baggage records");
            Console.WriteLine("2. Get record by ID");
            Console.WriteLine("3. List baggage by ticket");
            Console.WriteLine("4. Add baggage to ticket");
            Console.WriteLine("5. Update quantity and fee");
            Console.WriteLine("6. Remove baggage record");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();        break;
                case "2": await GetByIdAsync();        break;
                case "3": await ListByTicketAsync();   break;
                case "4": await AddBaggageAsync();     break;
                case "5": await UpdateAsync();         break;
                case "6": await RemoveAsync();         break;
                case "0": running = false;             break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var records = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Ticket Baggage Records ---");
        foreach (var r in records) PrintRecord(r);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter record ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var r = await _service.GetByIdAsync(id);
        if (r is null) Console.WriteLine($"Record with ID {id} not found.");
        else           PrintRecord(r);
    }

    private async Task ListByTicketAsync()
    {
        Console.Write("Enter Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid ID."); return; }

        var records = await _service.GetByTicketAsync(ticketId);
        var list    = records.ToList();
        Console.WriteLine($"\n--- Baggage for Ticket {ticketId} ---");

        decimal totalFee = list.Sum(r => r.FeeCharged);
        foreach (var r in list) PrintRecord(r);
        Console.WriteLine($"  Total baggage fee: {totalFee:F2}");
    }

    private async Task AddBaggageAsync()
    {
        Console.Write("Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Baggage Type ID: ");
        if (!int.TryParse(Console.ReadLine(), out int baggageTypeId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Quantity (> 0, default 1): ");
        if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
            qty = 1;

        Console.Write("Fee charged (>= 0, default 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
            fee = 0m;

        try
        {
            var created = await _service.CreateAsync(ticketId, baggageTypeId, qty, fee);
            Console.WriteLine(
                $"Baggage added: [{created.Id}] Ticket:{created.TicketId} | " +
                $"Type:{created.BaggageTypeId} | Qty:{created.Quantity} | Fee:{created.FeeCharged:F2}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task UpdateAsync()
    {
        Console.Write("Record ID to update: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New quantity (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
        { Console.WriteLine("Invalid quantity."); return; }

        Console.Write("New fee charged (>= 0): ");
        if (!decimal.TryParse(Console.ReadLine()?.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out decimal fee))
        { Console.WriteLine("Invalid fee."); return; }

        try
        {
            await _service.UpdateQuantityAndFeeAsync(id, qty, fee);
            Console.WriteLine("Baggage record updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task RemoveAsync()
    {
        Console.Write("Record ID to remove: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Baggage record removed successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintRecord(TicketBaggageDto r)
        => Console.WriteLine(
            $"  [{r.Id}] Ticket:{r.TicketId} | Type:{r.BaggageTypeId} | " +
            $"Qty:{r.Quantity} | Fee:{r.FeeCharged:F2}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── TicketBaggage Module ──────────────────────────────────────────────────────
builder.Services.AddScoped<ITicketBaggageRepository, TicketBaggageRepository>();
builder.Services.AddScoped<CreateTicketBaggageUseCase>();
builder.Services.AddScoped<DeleteTicketBaggageUseCase>();
builder.Services.AddScoped<GetAllTicketBaggagesUseCase>();
builder.Services.AddScoped<GetTicketBaggageByIdUseCase>();
builder.Services.AddScoped<UpdateTicketBaggageUseCase>();
builder.Services.AddScoped<GetTicketBaggagesByTicketUseCase>();
builder.Services.AddScoped<ITicketBaggageService, TicketBaggageService>();
builder.Services.AddScoped<TicketBaggageConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/TicketBaggage/
├── Application/
│   ├── Interfaces/
│   │   └── ITicketBaggageService.cs
│   ├── Services/
│   │   └── TicketBaggageService.cs
│   └── UseCases/
│       ├── CreateTicketBaggageUseCase.cs
│       ├── DeleteTicketBaggageUseCase.cs
│       ├── GetAllTicketBaggagesUseCase.cs
│       ├── GetTicketBaggageByIdUseCase.cs
│       ├── GetTicketBaggagesByTicketUseCase.cs
│       └── UpdateTicketBaggageUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── TicketBaggageAggregate.cs
│   ├── Repositories/
│   │   └── ITicketBaggageRepository.cs
│   └── valueObject/
│       └── TicketBaggageId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── TicketBaggageEntity.cs
│   │   └── TicketBaggageEntityConfiguration.cs
│   └── repository/
│       └── TicketBaggageRepository.cs
└── UI/
    └── TicketBaggageConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo TicketBaggage_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
