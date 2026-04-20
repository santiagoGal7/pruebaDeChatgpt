# Módulo: CheckIn
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn`  
**Raíz de archivos:** `src/Modules/CheckIn/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `check_in_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `ticket_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `ticket`. UNIQUE: un check-in por tiquete |
| `check_in_time` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Momento del check-in — inmutable |
| `check_in_status_id` | `INT NOT NULL FK` | `int` | FK → `check_in_status` |
| `counter_number` | `VARCHAR(20) NULL` | `string?` | Número de mostrador, nullable |

**UNIQUE:** `ticket_id` — un tiquete solo puede hacer check-in una vez.  
`ChangeStatus()` es la única mutación válida — también puede actualizar `counter_number`.  
`check_in_time` y `ticket_id` son inmutables tras la creación.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/CheckIn/Domain/valueObject/CheckInId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public sealed class CheckInId
{
    public int Value { get; }

    public CheckInId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CheckInId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is CheckInId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/CheckIn/Domain/aggregate/CheckInAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

/// <summary>
/// Registro del proceso de check-in de un pasajero.
/// SQL: check_in.
///
/// UNIQUE: ticket_id — un tiquete solo puede hacer check-in una vez.
/// check_in_time es inmutable tras la creación (registra el momento exacto).
///
/// ChangeStatus(): única mutación válida.
///   - Actualiza el estado del check-in (PENDING → CHECKED_IN → BOARDED).
///   - Opcionalmente actualiza el número de mostrador.
/// </summary>
public sealed class CheckInAggregate
{
    public CheckInId Id               { get; private set; }
    public int       TicketId         { get; private set; }
    public DateTime  CheckInTime      { get; private set; }
    public int       CheckInStatusId  { get; private set; }
    public string?   CounterNumber    { get; private set; }

    private CheckInAggregate()
    {
        Id = null!;
    }

    public CheckInAggregate(
        CheckInId id,
        int       ticketId,
        DateTime  checkInTime,
        int       checkInStatusId,
        string?   counterNumber = null)
    {
        if (ticketId <= 0)
            throw new ArgumentException(
                "TicketId must be a positive integer.", nameof(ticketId));

        if (checkInStatusId <= 0)
            throw new ArgumentException(
                "CheckInStatusId must be a positive integer.", nameof(checkInStatusId));

        ValidateCounterNumber(counterNumber);

        Id              = id;
        TicketId        = ticketId;
        CheckInTime     = checkInTime;
        CheckInStatusId = checkInStatusId;
        CounterNumber   = counterNumber?.Trim();
    }

    /// <summary>
    /// Actualiza el estado del check-in y opcionalmente el número de mostrador.
    /// ticket_id y check_in_time son inmutables.
    /// </summary>
    public void ChangeStatus(int checkInStatusId, string? counterNumber = null)
    {
        if (checkInStatusId <= 0)
            throw new ArgumentException(
                "CheckInStatusId must be a positive integer.", nameof(checkInStatusId));

        ValidateCounterNumber(counterNumber);

        CheckInStatusId = checkInStatusId;
        CounterNumber   = counterNumber?.Trim() ?? CounterNumber;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateCounterNumber(string? counterNumber)
    {
        if (counterNumber is not null && counterNumber.Trim().Length > 20)
            throw new ArgumentException(
                "CounterNumber cannot exceed 20 characters.", nameof(counterNumber));
    }
}
```

---

### RUTA: `src/Modules/CheckIn/Domain/Repositories/ICheckInRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public interface ICheckInRepository
{
    Task<CheckInAggregate?>             GetByIdAsync(CheckInId id,                         CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInAggregate>> GetAllAsync(                                        CancellationToken cancellationToken = default);
    Task<CheckInAggregate?>             GetByTicketAsync(int ticketId,                      CancellationToken cancellationToken = default);
    Task                                AddAsync(CheckInAggregate checkIn,                  CancellationToken cancellationToken = default);
    Task                                UpdateAsync(CheckInAggregate checkIn,               CancellationToken cancellationToken = default);
    Task                                DeleteAsync(CheckInId id,                           CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/CheckIn/Application/Interfaces/ICheckInService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;

public interface ICheckInService
{
    Task<CheckInDto?>             GetByIdAsync(int id,                                                               CancellationToken cancellationToken = default);
    Task<IEnumerable<CheckInDto>> GetAllAsync(                                                                       CancellationToken cancellationToken = default);
    Task<CheckInDto?>             GetByTicketAsync(int ticketId,                                                     CancellationToken cancellationToken = default);
    Task<CheckInDto>              CreateAsync(int ticketId, int checkInStatusId, string? counterNumber,              CancellationToken cancellationToken = default);
    Task                          ChangeStatusAsync(int id, int checkInStatusId, string? counterNumber,             CancellationToken cancellationToken = default);
    Task                          DeleteAsync(int id,                                                               CancellationToken cancellationToken = default);
}

public sealed record CheckInDto(
    int      Id,
    int      TicketId,
    DateTime CheckInTime,
    int      CheckInStatusId,
    string?  CounterNumber);
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/CreateCheckInUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCheckInUseCase
{
    private readonly ICheckInRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public CreateCheckInUseCase(ICheckInRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInAggregate> ExecuteAsync(
        int               ticketId,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        // CheckInId(1) es placeholder; EF Core asigna el Id real al insertar.
        var checkIn = new CheckInAggregate(
            new CheckInId(1),
            ticketId,
            DateTime.UtcNow,
            checkInStatusId,
            counterNumber);

        await _repository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return checkIn;
    }
}
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/DeleteCheckInUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCheckInUseCase
{
    private readonly ICheckInRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public DeleteCheckInUseCase(ICheckInRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CheckInId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/GetAllCheckInsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

public sealed class GetAllCheckInsUseCase
{
    private readonly ICheckInRepository _repository;

    public GetAllCheckInsUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CheckInAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/GetCheckInByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public sealed class GetCheckInByIdUseCase
{
    private readonly ICheckInRepository _repository;

    public GetCheckInByIdUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CheckInId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/ChangeCheckInStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia el estado del check-in (PENDING → CHECKED_IN → BOARDED, etc.)
/// y opcionalmente actualiza el número de mostrador.
/// ticket_id y check_in_time son inmutables.
/// </summary>
public sealed class ChangeCheckInStatusUseCase
{
    private readonly ICheckInRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public ChangeCheckInStatusUseCase(ICheckInRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        var checkIn = await _repository.GetByIdAsync(new CheckInId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CheckIn with id {id} was not found.");

        checkIn.ChangeStatus(checkInStatusId, counterNumber);
        await _repository.UpdateAsync(checkIn, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/CheckIn/Application/UseCases/GetCheckInByTicketUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

/// <summary>
/// Obtiene el check-in de un tiquete.
/// La UNIQUE sobre ticket_id garantiza como máximo un resultado.
/// Útil para verificar si un pasajero ya realizó check-in antes de
/// emitir el boarding pass.
/// </summary>
public sealed class GetCheckInByTicketUseCase
{
    private readonly ICheckInRepository _repository;

    public GetCheckInByTicketUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInAggregate?> ExecuteAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByTicketAsync(ticketId, cancellationToken);
}
```

---

### RUTA: `src/Modules/CheckIn/Application/Services/CheckInService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;

public sealed class CheckInService : ICheckInService
{
    private readonly CreateCheckInUseCase       _create;
    private readonly DeleteCheckInUseCase       _delete;
    private readonly GetAllCheckInsUseCase      _getAll;
    private readonly GetCheckInByIdUseCase      _getById;
    private readonly ChangeCheckInStatusUseCase _changeStatus;
    private readonly GetCheckInByTicketUseCase  _getByTicket;

    public CheckInService(
        CreateCheckInUseCase      create,
        DeleteCheckInUseCase      delete,
        GetAllCheckInsUseCase     getAll,
        GetCheckInByIdUseCase     getById,
        ChangeCheckInStatusUseCase changeStatus,
        GetCheckInByTicketUseCase getByTicket)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _changeStatus = changeStatus;
        _getByTicket  = getByTicket;
    }

    public async Task<CheckInDto> CreateAsync(
        int               ticketId,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(
            ticketId, checkInStatusId, counterNumber, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CheckInDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CheckInDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeStatusAsync(
        int               id,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
        => await _changeStatus.ExecuteAsync(id, checkInStatusId, counterNumber, cancellationToken);

    public async Task<CheckInDto?> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByTicket.ExecuteAsync(ticketId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CheckInDto ToDto(CheckInAggregate agg)
        => new(
            agg.Id.Value,
            agg.TicketId,
            agg.CheckInTime,
            agg.CheckInStatusId,
            agg.CounterNumber);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/CheckIn/Infrastructure/entity/CheckInEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Entity;

public sealed class CheckInEntity
{
    public int      Id              { get; set; }
    public int      TicketId        { get; set; }
    public DateTime CheckInTime     { get; set; }
    public int      CheckInStatusId { get; set; }
    public string?  CounterNumber   { get; set; }
}
```

---

### RUTA: `src/Modules/CheckIn/Infrastructure/entity/CheckInEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CheckInEntityConfiguration : IEntityTypeConfiguration<CheckInEntity>
{
    public void Configure(EntityTypeBuilder<CheckInEntity> builder)
    {
        builder.ToTable("check_in");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("check_in_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.TicketId)
               .HasColumnName("ticket_id")
               .IsRequired();

        // UNIQUE (ticket_id) — un tiquete = un check-in
        builder.HasIndex(e => e.TicketId)
               .IsUnique()
               .HasDatabaseName("uq_check_in_ticket");

        builder.Property(e => e.CheckInTime)
               .HasColumnName("check_in_time")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CheckInStatusId)
               .HasColumnName("check_in_status_id")
               .IsRequired();

        builder.Property(e => e.CounterNumber)
               .HasColumnName("counter_number")
               .IsRequired(false)
               .HasMaxLength(20);
    }
}
```

---

### RUTA: `src/Modules/CheckIn/Infrastructure/repository/CheckInRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CheckInRepository : ICheckInRepository
{
    private readonly AppDbContext _context;

    public CheckInRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CheckInAggregate ToDomain(CheckInEntity entity)
        => new(
            new CheckInId(entity.Id),
            entity.TicketId,
            entity.CheckInTime,
            entity.CheckInStatusId,
            entity.CounterNumber);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CheckInAggregate?> GetByIdAsync(
        CheckInId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckIns
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CheckInAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.CheckIns
            .AsNoTracking()
            .OrderByDescending(e => e.CheckInTime)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<CheckInAggregate?> GetByTicketAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
    {
        // ticket_id es UNIQUE — FirstOrDefault es correcto.
        var entity = await _context.CheckIns
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TicketId == ticketId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        CheckInAggregate  checkIn,
        CancellationToken cancellationToken = default)
    {
        var entity = new CheckInEntity
        {
            TicketId        = checkIn.TicketId,
            CheckInTime     = checkIn.CheckInTime,
            CheckInStatusId = checkIn.CheckInStatusId,
            CounterNumber   = checkIn.CounterNumber
        };
        await _context.CheckIns.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CheckInAggregate  checkIn,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckIns
            .FirstOrDefaultAsync(e => e.Id == checkIn.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInEntity with id {checkIn.Id.Value} not found.");

        // Solo CheckInStatusId y CounterNumber son mutables.
        // TicketId y CheckInTime son inmutables.
        entity.CheckInStatusId = checkIn.CheckInStatusId;
        entity.CounterNumber   = checkIn.CounterNumber;

        _context.CheckIns.Update(entity);
    }

    public async Task DeleteAsync(
        CheckInId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckIns
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInEntity with id {id.Value} not found.");

        _context.CheckIns.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/CheckIn/UI/CheckInConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.Interfaces;

public sealed class CheckInConsoleUI
{
    private readonly ICheckInService _service;

    public CheckInConsoleUI(ICheckInService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== CHECK-IN MODULE ==========");
            Console.WriteLine("1. List all check-ins");
            Console.WriteLine("2. Get check-in by ID");
            Console.WriteLine("3. Get check-in by ticket");
            Console.WriteLine("4. Register check-in");
            Console.WriteLine("5. Change check-in status");
            Console.WriteLine("6. Delete check-in");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();         break;
                case "2": await GetByIdAsync();         break;
                case "3": await GetByTicketAsync();     break;
                case "4": await RegisterAsync();        break;
                case "5": await ChangeStatusAsync();    break;
                case "6": await DeleteAsync();          break;
                case "0": running = false;              break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var checkIns = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Check-Ins ---");
        foreach (var c in checkIns) PrintCheckIn(c);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter check-in ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByIdAsync(id);
        if (c is null) Console.WriteLine($"Check-in with ID {id} not found.");
        else           PrintCheckIn(c);
    }

    private async Task GetByTicketAsync()
    {
        Console.Write("Enter Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid ID."); return; }

        var c = await _service.GetByTicketAsync(ticketId);
        if (c is null)
            Console.WriteLine($"Ticket {ticketId} has not checked in yet.");
        else
            PrintCheckIn(c);
    }

    private async Task RegisterAsync()
    {
        Console.Write("Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int ticketId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Initial Check-In Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Counter number (optional): ");
        var counterInput = Console.ReadLine()?.Trim();
        string? counter = string.IsNullOrWhiteSpace(counterInput) ? null : counterInput;

        try
        {
            var created = await _service.CreateAsync(ticketId, statusId, counter);
            Console.WriteLine(
                $"Check-in registered: [{created.Id}] Ticket:{created.TicketId} | " +
                $"Status:{created.CheckInStatusId} | " +
                $"Time:{created.CheckInTime:yyyy-MM-dd HH:mm}" +
                (created.CounterNumber is not null ? $" | Counter:{created.CounterNumber}" : string.Empty));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task ChangeStatusAsync()
    {
        Console.Write("Check-in ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("New Check-In Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid."); return; }

        Console.Write("Counter number (optional, Enter to keep current): ");
        var counterInput = Console.ReadLine()?.Trim();
        string? counter = string.IsNullOrWhiteSpace(counterInput) ? null : counterInput;

        try
        {
            await _service.ChangeStatusAsync(id, statusId, counter);
            Console.WriteLine("Check-in status updated successfully.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task DeleteAsync()
    {
        Console.Write("Check-in ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Check-in deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintCheckIn(CheckInDto c)
        => Console.WriteLine(
            $"  [{c.Id}] Ticket:{c.TicketId} | Status:{c.CheckInStatusId} | " +
            $"Time:{c.CheckInTime:yyyy-MM-dd HH:mm}" +
            (c.CounterNumber is not null ? $" | Counter:{c.CounterNumber}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── CheckIn Module ────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<CreateCheckInUseCase>();
builder.Services.AddScoped<DeleteCheckInUseCase>();
builder.Services.AddScoped<GetAllCheckInsUseCase>();
builder.Services.AddScoped<GetCheckInByIdUseCase>();
builder.Services.AddScoped<ChangeCheckInStatusUseCase>();
builder.Services.AddScoped<GetCheckInByTicketUseCase>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<CheckInConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/CheckIn/
├── Application/
│   ├── Interfaces/
│   │   └── ICheckInService.cs
│   ├── Services/
│   │   └── CheckInService.cs
│   └── UseCases/
│       ├── ChangeCheckInStatusUseCase.cs
│       ├── CreateCheckInUseCase.cs
│       ├── DeleteCheckInUseCase.cs
│       ├── GetAllCheckInsUseCase.cs
│       ├── GetCheckInByIdUseCase.cs
│       └── GetCheckInByTicketUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── CheckInAggregate.cs
│   ├── Repositories/
│   │   └── ICheckInRepository.cs
│   └── valueObject/
│       └── CheckInId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── CheckInEntity.cs
│   │   └── CheckInEntityConfiguration.cs
│   └── repository/
│       └── CheckInRepository.cs
└── UI/
    └── CheckInConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo CheckIn_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
