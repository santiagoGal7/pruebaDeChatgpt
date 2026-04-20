# Módulo: LoyaltyTransaction
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction`  
**Raíz de archivos:** `src/Modules/LoyaltyTransaction/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `loyalty_transaction_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd. Renombrado [NC-11] |
| `loyalty_account_id` | `INT NOT NULL FK` | `int` | FK → `loyalty_account` |
| `ticket_id` | `INT NULL FK` | `int?` | FK → `ticket`. Nullable (puede ser ajuste manual) |
| `transaction_type` | `VARCHAR(10) NOT NULL` | `string` | CHECK `IN ('EARN', 'REDEEM')` |
| `miles` | `INT NOT NULL` | `int` | CHECK `> 0` — siempre positivo |
| `transaction_date` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Inmutable tras creación |

**CHECKs del DDL espejados en dominio:**
- `transaction_type IN ('EARN', 'REDEEM')`.
- `miles > 0` — siempre positivo; la dirección la da `transaction_type`.

Tabla de **solo lectura** tras inserción — no hay mutación. El historial de millas es inmutable (auditoría).  
Sin `updated_at` en el DDL.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/LoyaltyTransaction/Domain/valueObject/LoyaltyTransactionId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;

public sealed class LoyaltyTransactionId
{
    public int Value { get; }

    public LoyaltyTransactionId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("LoyaltyTransactionId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is LoyaltyTransactionId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Domain/aggregate/LoyaltyTransactionAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;

/// <summary>
/// Registro de acumulación o redención de millas en una cuenta de fidelización.
/// SQL: loyalty_transaction. [NC-11] id renombrado a loyalty_transaction_id.
///
/// CHECKs del DDL espejados:
///   - transaction_type IN ('EARN', 'REDEEM').
///   - miles > 0 (siempre positivo; la dirección la da transaction_type).
///
/// Tabla de auditoría — INMUTABLE tras inserción.
/// No se provee ningún método de mutación; el repositorio no expone UpdateAsync.
/// ticket_id es nullable: si es null, la transacción es un ajuste manual.
/// </summary>
public sealed class LoyaltyTransactionAggregate
{
    public static readonly string TypeEarn   = "EARN";
    public static readonly string TypeRedeem = "REDEEM";

    public LoyaltyTransactionId Id               { get; private set; }
    public int                  LoyaltyAccountId { get; private set; }
    public int?                 TicketId         { get; private set; }
    public string               TransactionType  { get; private set; }
    public int                  Miles            { get; private set; }
    public DateTime             TransactionDate  { get; private set; }

    private LoyaltyTransactionAggregate()
    {
        Id              = null!;
        TransactionType = null!;
    }

    public LoyaltyTransactionAggregate(
        LoyaltyTransactionId id,
        int                  loyaltyAccountId,
        int?                 ticketId,
        string               transactionType,
        int                  miles,
        DateTime             transactionDate)
    {
        if (loyaltyAccountId <= 0)
            throw new ArgumentException(
                "LoyaltyAccountId must be a positive integer.", nameof(loyaltyAccountId));

        if (ticketId.HasValue && ticketId.Value <= 0)
            throw new ArgumentException(
                "TicketId must be a positive integer when provided.", nameof(ticketId));

        ValidateTransactionType(transactionType);
        ValidateMiles(miles);

        Id               = id;
        LoyaltyAccountId = loyaltyAccountId;
        TicketId         = ticketId;
        TransactionType  = transactionType.Trim().ToUpperInvariant();
        Miles            = miles;
        TransactionDate  = transactionDate;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateTransactionType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException(
                "TransactionType cannot be empty.", nameof(type));

        var normalized = type.Trim().ToUpperInvariant();
        if (normalized != TypeEarn && normalized != TypeRedeem)
            throw new ArgumentException(
                $"TransactionType must be '{TypeEarn}' or '{TypeRedeem}'. [chk_ltx_type]",
                nameof(type));
    }

    private static void ValidateMiles(int miles)
    {
        if (miles <= 0)
            throw new ArgumentException(
                "Miles must be greater than 0. [chk_ltx_miles]", nameof(miles));
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Domain/Repositories/ILoyaltyTransactionRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;

public interface ILoyaltyTransactionRepository
{
    Task<LoyaltyTransactionAggregate?>             GetByIdAsync(LoyaltyTransactionId id,                     CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTransactionAggregate>> GetAllAsync(                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTransactionAggregate>> GetByAccountAsync(int loyaltyAccountId,                   CancellationToken cancellationToken = default);
    Task                                           AddAsync(LoyaltyTransactionAggregate loyaltyTransaction,  CancellationToken cancellationToken = default);
    Task                                           DeleteAsync(LoyaltyTransactionId id,                      CancellationToken cancellationToken = default);
    // UpdateAsync deliberadamente omitido — tabla de auditoría inmutable.
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/Interfaces/ILoyaltyTransactionService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.Interfaces;

public interface ILoyaltyTransactionService
{
    Task<LoyaltyTransactionDto?>             GetByIdAsync(int id,                                                                   CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTransactionDto>> GetAllAsync(                                                                           CancellationToken cancellationToken = default);
    Task<IEnumerable<LoyaltyTransactionDto>> GetByAccountAsync(int loyaltyAccountId,                                               CancellationToken cancellationToken = default);
    Task<LoyaltyTransactionDto>              EarnAsync(int loyaltyAccountId, int? ticketId, int miles,                             CancellationToken cancellationToken = default);
    Task<LoyaltyTransactionDto>              RedeemAsync(int loyaltyAccountId, int? ticketId, int miles,                           CancellationToken cancellationToken = default);
    Task                                     DeleteAsync(int id,                                                                   CancellationToken cancellationToken = default);
}

public sealed record LoyaltyTransactionDto(
    int      Id,
    int      LoyaltyAccountId,
    int?     TicketId,
    string   TransactionType,
    int      Miles,
    DateTime TransactionDate);
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/EarnMilesTransactionUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Registra una transacción de tipo EARN (acumulación de millas).
/// El saldo de la cuenta debe actualizarse por separado vía LoyaltyAccount.AddMiles().
/// </summary>
public sealed class EarnMilesTransactionUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public EarnMilesTransactionUseCase(ILoyaltyTransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyTransactionAggregate> ExecuteAsync(
        int               loyaltyAccountId,
        int?              ticketId,
        int               miles,
        CancellationToken cancellationToken = default)
    {
        var transaction = new LoyaltyTransactionAggregate(
            new LoyaltyTransactionId(1),
            loyaltyAccountId,
            ticketId,
            LoyaltyTransactionAggregate.TypeEarn,
            miles,
            DateTime.UtcNow);

        await _repository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return transaction;
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/RedeemMilesTransactionUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Registra una transacción de tipo REDEEM (redención de millas).
/// El saldo de la cuenta debe actualizarse por separado vía LoyaltyAccount.RedeemMiles().
/// </summary>
public sealed class RedeemMilesTransactionUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public RedeemMilesTransactionUseCase(ILoyaltyTransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoyaltyTransactionAggregate> ExecuteAsync(
        int               loyaltyAccountId,
        int?              ticketId,
        int               miles,
        CancellationToken cancellationToken = default)
    {
        var transaction = new LoyaltyTransactionAggregate(
            new LoyaltyTransactionId(1),
            loyaltyAccountId,
            ticketId,
            LoyaltyTransactionAggregate.TypeRedeem,
            miles,
            DateTime.UtcNow);

        await _repository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return transaction;
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/DeleteLoyaltyTransactionUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteLoyaltyTransactionUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public DeleteLoyaltyTransactionUseCase(ILoyaltyTransactionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new LoyaltyTransactionId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/GetAllLoyaltyTransactionsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;

public sealed class GetAllLoyaltyTransactionsUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;

    public GetAllLoyaltyTransactionsUseCase(ILoyaltyTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyTransactionAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/GetLoyaltyTransactionByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;

public sealed class GetLoyaltyTransactionByIdUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;

    public GetLoyaltyTransactionByIdUseCase(ILoyaltyTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<LoyaltyTransactionAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new LoyaltyTransactionId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/UseCases/GetLoyaltyTransactionsByAccountUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;

/// <summary>
/// Historial de todas las transacciones (EARN y REDEEM) de una cuenta.
/// Caso de uso clave para mostrar el estado del saldo de millas.
/// </summary>
public sealed class GetLoyaltyTransactionsByAccountUseCase
{
    private readonly ILoyaltyTransactionRepository _repository;

    public GetLoyaltyTransactionsByAccountUseCase(ILoyaltyTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LoyaltyTransactionAggregate>> ExecuteAsync(
        int               loyaltyAccountId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByAccountAsync(loyaltyAccountId, cancellationToken);
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Application/Services/LoyaltyTransactionService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;

public sealed class LoyaltyTransactionService : ILoyaltyTransactionService
{
    private readonly EarnMilesTransactionUseCase             _earn;
    private readonly RedeemMilesTransactionUseCase           _redeem;
    private readonly DeleteLoyaltyTransactionUseCase         _delete;
    private readonly GetAllLoyaltyTransactionsUseCase        _getAll;
    private readonly GetLoyaltyTransactionByIdUseCase        _getById;
    private readonly GetLoyaltyTransactionsByAccountUseCase  _getByAccount;

    public LoyaltyTransactionService(
        EarnMilesTransactionUseCase            earn,
        RedeemMilesTransactionUseCase          redeem,
        DeleteLoyaltyTransactionUseCase        delete,
        GetAllLoyaltyTransactionsUseCase       getAll,
        GetLoyaltyTransactionByIdUseCase       getById,
        GetLoyaltyTransactionsByAccountUseCase getByAccount)
    {
        _earn         = earn;
        _redeem       = redeem;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _getByAccount = getByAccount;
    }

    public async Task<LoyaltyTransactionDto> EarnAsync(
        int               loyaltyAccountId,
        int?              ticketId,
        int               miles,
        CancellationToken cancellationToken = default)
    {
        var agg = await _earn.ExecuteAsync(loyaltyAccountId, ticketId, miles, cancellationToken);
        return ToDto(agg);
    }

    public async Task<LoyaltyTransactionDto> RedeemAsync(
        int               loyaltyAccountId,
        int?              ticketId,
        int               miles,
        CancellationToken cancellationToken = default)
    {
        var agg = await _redeem.ExecuteAsync(loyaltyAccountId, ticketId, miles, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<LoyaltyTransactionDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<LoyaltyTransactionDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task<IEnumerable<LoyaltyTransactionDto>> GetByAccountAsync(
        int               loyaltyAccountId,
        CancellationToken cancellationToken = default)
    {
        var list = await _getByAccount.ExecuteAsync(loyaltyAccountId, cancellationToken);
        return list.Select(ToDto);
    }

    private static LoyaltyTransactionDto ToDto(LoyaltyTransactionAggregate agg)
        => new(
            agg.Id.Value,
            agg.LoyaltyAccountId,
            agg.TicketId,
            agg.TransactionType,
            agg.Miles,
            agg.TransactionDate);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/LoyaltyTransaction/Infrastructure/entity/LoyaltyTransactionEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Infrastructure.Entity;

public sealed class LoyaltyTransactionEntity
{
    public int      Id               { get; set; }
    public int      LoyaltyAccountId { get; set; }
    public int?     TicketId         { get; set; }
    public string   TransactionType  { get; set; } = null!;
    public int      Miles            { get; set; }
    public DateTime TransactionDate  { get; set; }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Infrastructure/entity/LoyaltyTransactionEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class LoyaltyTransactionEntityConfiguration
    : IEntityTypeConfiguration<LoyaltyTransactionEntity>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransactionEntity> builder)
    {
        builder.ToTable("loyalty_transaction");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("loyalty_transaction_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.LoyaltyAccountId)
               .HasColumnName("loyalty_account_id")
               .IsRequired();

        builder.Property(e => e.TicketId)
               .HasColumnName("ticket_id")
               .IsRequired(false);

        builder.Property(e => e.TransactionType)
               .HasColumnName("transaction_type")
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(e => e.Miles)
               .HasColumnName("miles")
               .IsRequired();

        builder.Property(e => e.TransactionDate)
               .HasColumnName("transaction_date")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

---

### RUTA: `src/Modules/LoyaltyTransaction/Infrastructure/repository/LoyaltyTransactionRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class LoyaltyTransactionRepository : ILoyaltyTransactionRepository
{
    private readonly AppDbContext _context;

    public LoyaltyTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    private static LoyaltyTransactionAggregate ToDomain(LoyaltyTransactionEntity entity)
        => new(
            new LoyaltyTransactionId(entity.Id),
            entity.LoyaltyAccountId,
            entity.TicketId,
            entity.TransactionType,
            entity.Miles,
            entity.TransactionDate);

    public async Task<LoyaltyTransactionAggregate?> GetByIdAsync(
        LoyaltyTransactionId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.LoyaltyTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<LoyaltyTransactionAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyTransactions
            .AsNoTracking()
            .OrderByDescending(e => e.TransactionDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<IEnumerable<LoyaltyTransactionAggregate>> GetByAccountAsync(
        int               loyaltyAccountId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.LoyaltyTransactions
            .AsNoTracking()
            .Where(e => e.LoyaltyAccountId == loyaltyAccountId)
            .OrderByDescending(e => e.TransactionDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        LoyaltyTransactionAggregate loyaltyTransaction,
        CancellationToken           cancellationToken = default)
    {
        var entity = new LoyaltyTransactionEntity
        {
            LoyaltyAccountId = loyaltyTransaction.LoyaltyAccountId,
            TicketId         = loyaltyTransaction.TicketId,
            TransactionType  = loyaltyTransaction.TransactionType,
            Miles            = loyaltyTransaction.Miles,
            TransactionDate  = loyaltyTransaction.TransactionDate
        };
        await _context.LoyaltyTransactions.AddAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(
        LoyaltyTransactionId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.LoyaltyTransactions
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"LoyaltyTransactionEntity with id {id.Value} not found.");

        _context.LoyaltyTransactions.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/LoyaltyTransaction/UI/LoyaltyTransactionConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.LoyaltyTransaction.Application.Interfaces;

public sealed class LoyaltyTransactionConsoleUI
{
    private readonly ILoyaltyTransactionService _service;

    public LoyaltyTransactionConsoleUI(ILoyaltyTransactionService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== LOYALTY TRANSACTION MODULE ==========");
            Console.WriteLine("1. List all transactions");
            Console.WriteLine("2. Get transaction by ID");
            Console.WriteLine("3. List transactions by account");
            Console.WriteLine("4. Record EARN (miles accrual)");
            Console.WriteLine("5. Record REDEEM (miles redemption)");
            Console.WriteLine("6. Delete transaction record");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();        break;
                case "2": await GetByIdAsync();        break;
                case "3": await ListByAccountAsync();  break;
                case "4": await RecordEarnAsync();     break;
                case "5": await RecordRedeemAsync();   break;
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
        var txs = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Loyalty Transactions ---");
        foreach (var t in txs) PrintTx(t);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter transaction ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByIdAsync(id);
        if (t is null) Console.WriteLine($"Transaction with ID {id} not found.");
        else           PrintTx(t);
    }

    private async Task ListByAccountAsync()
    {
        Console.Write("Enter Loyalty Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int accountId))
        { Console.WriteLine("Invalid ID."); return; }

        var txs = await _service.GetByAccountAsync(accountId);
        var list = txs.ToList();

        Console.WriteLine($"\n--- Transactions for Account {accountId} ---");

        int totalEarned  = list.Where(t => t.TransactionType == "EARN").Sum(t => t.Miles);
        int totalRedeemed = list.Where(t => t.TransactionType == "REDEEM").Sum(t => t.Miles);

        foreach (var t in list) PrintTx(t);
        Console.WriteLine($"  Earned: {totalEarned:N0} | Redeemed: {totalRedeemed:N0}");
    }

    private async Task RecordEarnAsync()
    {
        Console.Write("Loyalty Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int accountId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Ticket ID (optional): ");
        var ticketInput = Console.ReadLine()?.Trim();
        int? ticketId = int.TryParse(ticketInput, out int tParsed) ? tParsed : null;

        Console.Write("Miles to earn (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int miles) || miles <= 0)
        { Console.WriteLine("Invalid miles."); return; }

        try
        {
            var created = await _service.EarnAsync(accountId, ticketId, miles);
            Console.WriteLine($"EARN recorded: [{created.Id}] {created.Miles:N0} miles | " +
                              $"Account:{created.LoyaltyAccountId} | {created.TransactionDate:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task RecordRedeemAsync()
    {
        Console.Write("Loyalty Account ID: ");
        if (!int.TryParse(Console.ReadLine(), out int accountId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Ticket ID (optional): ");
        var ticketInput = Console.ReadLine()?.Trim();
        int? ticketId = int.TryParse(ticketInput, out int tParsed) ? tParsed : null;

        Console.Write("Miles to redeem (> 0): ");
        if (!int.TryParse(Console.ReadLine(), out int miles) || miles <= 0)
        { Console.WriteLine("Invalid miles."); return; }

        try
        {
            var created = await _service.RedeemAsync(accountId, ticketId, miles);
            Console.WriteLine($"REDEEM recorded: [{created.Id}] {created.Miles:N0} miles | " +
                              $"Account:{created.LoyaltyAccountId} | {created.TransactionDate:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex) { Console.WriteLine($"Validation error: {ex.Message}"); }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Transaction ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Transaction record deleted successfully.");
    }

    private static void PrintTx(LoyaltyTransactionDto t)
        => Console.WriteLine(
            $"  [{t.Id}] {t.TransactionType,6} | {t.Miles,6:N0} miles | " +
            $"Account:{t.LoyaltyAccountId} | " +
            (t.TicketId.HasValue ? $"Ticket:{t.TicketId} | " : string.Empty) +
            $"{t.TransactionDate:yyyy-MM-dd HH:mm}");
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento)_

```csharp
// ── LoyaltyTransaction Module ─────────────────────────────────────────────────
builder.Services.AddScoped<ILoyaltyTransactionRepository, LoyaltyTransactionRepository>();
builder.Services.AddScoped<EarnMilesTransactionUseCase>();
builder.Services.AddScoped<RedeemMilesTransactionUseCase>();
builder.Services.AddScoped<DeleteLoyaltyTransactionUseCase>();
builder.Services.AddScoped<GetAllLoyaltyTransactionsUseCase>();
builder.Services.AddScoped<GetLoyaltyTransactionByIdUseCase>();
builder.Services.AddScoped<GetLoyaltyTransactionsByAccountUseCase>();
builder.Services.AddScoped<ILoyaltyTransactionService, LoyaltyTransactionService>();
builder.Services.AddScoped<LoyaltyTransactionConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/LoyaltyTransaction/
├── Application/
│   ├── Interfaces/
│   │   └── ILoyaltyTransactionService.cs
│   ├── Services/
│   │   └── LoyaltyTransactionService.cs
│   └── UseCases/
│       ├── DeleteLoyaltyTransactionUseCase.cs
│       ├── EarnMilesTransactionUseCase.cs
│       ├── GetAllLoyaltyTransactionsUseCase.cs
│       ├── GetLoyaltyTransactionByIdUseCase.cs
│       ├── GetLoyaltyTransactionsByAccountUseCase.cs
│       └── RedeemMilesTransactionUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── LoyaltyTransactionAggregate.cs
│   ├── Repositories/
│   │   └── ILoyaltyTransactionRepository.cs
│   └── valueObject/
│       └── LoyaltyTransactionId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── LoyaltyTransactionEntity.cs
│   │   └── LoyaltyTransactionEntityConfiguration.cs
│   └── repository/
│       └── LoyaltyTransactionRepository.cs
└── UI/
    └── LoyaltyTransactionConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo LoyaltyTransaction_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
