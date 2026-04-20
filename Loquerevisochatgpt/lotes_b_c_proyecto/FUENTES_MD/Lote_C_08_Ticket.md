# Módulo: Ticket
**Proyecto:** Sistema_de_gestion_de_tiquetes_Aereos  
**Arquitectura:** Hexagonal Pura  
**Namespace base:** `Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket`  
**Raíz de archivos:** `src/Modules/Ticket/`

---

## NOTAS DE DISEÑO

| Columna SQL | Tipo SQL | Tipo C# | Observación |
|---|---|---|---|
| `ticket_id` | `INT AUTO_INCREMENT PK` | `int` | ValueGeneratedOnAdd |
| `ticket_code` | `VARCHAR(30) NOT NULL UNIQUE` | `string` | Código único del tiquete |
| `reservation_detail_id` | `INT NOT NULL UNIQUE FK` | `int` | FK → `reservation_detail`. Un tiquete = una línea de reserva. UNIQUE |
| `issue_date` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Fecha de emisión — inmutable |
| `ticket_status_id` | `INT NOT NULL FK` | `int` | FK → `ticket_status` |
| `created_at` | `TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP` | `DateTime` | Solo lectura |
| `updated_at` | `DATETIME NULL` | `DateTime?` | Nullable |

**UNIQUE:** `ticket_code` y `reservation_detail_id` — un tiquete por línea de reserva.  
`ChangeStatus()` es la única mutación de negocio válida.  
`ticket_code` se normaliza a `ToUpperInvariant()`.

---

## 1. DOMAIN

---

### RUTA: `src/Modules/Ticket/Domain/valueObject/TicketId.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;

public sealed class TicketId
{
    public int Value { get; }

    public TicketId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("TicketId must be a positive integer.", nameof(value));

        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is TicketId other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
```

---

### RUTA: `src/Modules/Ticket/Domain/aggregate/TicketAggregate.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;

/// <summary>
/// Tiquete aéreo emitido para un pasajero sobre una línea de reserva.
/// SQL: ticket.
///
/// UNIQUE: ticket_code — código de negocio del tiquete.
/// UNIQUE: reservation_detail_id — un tiquete por línea de reserva.
///
/// ticket_code normalizado a mayúsculas.
/// issue_date y reservation_detail_id son inmutables tras la emisión.
/// ChangeStatus(): única mutación válida sobre el ciclo de vida del tiquete.
/// </summary>
public sealed class TicketAggregate
{
    public TicketId  Id                  { get; private set; }
    public string    TicketCode          { get; private set; }
    public int       ReservationDetailId { get; private set; }
    public DateTime  IssueDate           { get; private set; }
    public int       TicketStatusId      { get; private set; }
    public DateTime  CreatedAt           { get; private set; }
    public DateTime? UpdatedAt           { get; private set; }

    private TicketAggregate()
    {
        Id         = null!;
        TicketCode = null!;
    }

    public TicketAggregate(
        TicketId  id,
        string    ticketCode,
        int       reservationDetailId,
        DateTime  issueDate,
        int       ticketStatusId,
        DateTime  createdAt,
        DateTime? updatedAt = null)
    {
        ValidateTicketCode(ticketCode);

        if (reservationDetailId <= 0)
            throw new ArgumentException(
                "ReservationDetailId must be a positive integer.", nameof(reservationDetailId));

        if (ticketStatusId <= 0)
            throw new ArgumentException(
                "TicketStatusId must be a positive integer.", nameof(ticketStatusId));

        Id                  = id;
        TicketCode          = ticketCode.Trim().ToUpperInvariant();
        ReservationDetailId = reservationDetailId;
        IssueDate           = issueDate;
        TicketStatusId      = ticketStatusId;
        CreatedAt           = createdAt;
        UpdatedAt           = updatedAt;
    }

    /// <summary>
    /// Cambia el estado del tiquete (ISSUED → USED, ISSUED → CANCELLED, etc.).
    /// ticket_code, reservation_detail_id e issue_date son inmutables.
    /// </summary>
    public void ChangeStatus(int ticketStatusId)
    {
        if (ticketStatusId <= 0)
            throw new ArgumentException(
                "TicketStatusId must be a positive integer.", nameof(ticketStatusId));

        TicketStatusId = ticketStatusId;
        UpdatedAt      = DateTime.UtcNow;
    }

    // ── Validaciones privadas ─────────────────────────────────────────────────

    private static void ValidateTicketCode(string ticketCode)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            throw new ArgumentException("TicketCode cannot be empty.", nameof(ticketCode));

        if (ticketCode.Trim().Length > 30)
            throw new ArgumentException(
                "TicketCode cannot exceed 30 characters.", nameof(ticketCode));
    }
}
```

---

### RUTA: `src/Modules/Ticket/Domain/Repositories/ITicketRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;

public interface ITicketRepository
{
    Task<TicketAggregate?>             GetByIdAsync(TicketId id,                              CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketAggregate>> GetAllAsync(                                            CancellationToken cancellationToken = default);
    Task<TicketAggregate?>             GetByReservationDetailAsync(int reservationDetailId,    CancellationToken cancellationToken = default);
    Task                               AddAsync(TicketAggregate ticket,                        CancellationToken cancellationToken = default);
    Task                               UpdateAsync(TicketAggregate ticket,                     CancellationToken cancellationToken = default);
    Task                               DeleteAsync(TicketId id,                                CancellationToken cancellationToken = default);
}
```

---

## 2. APPLICATION

---

### RUTA: `src/Modules/Ticket/Application/Interfaces/ITicketService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.Interfaces;

public interface ITicketService
{
    Task<TicketDto?>             GetByIdAsync(int id,                                                         CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketDto>> GetAllAsync(                                                                 CancellationToken cancellationToken = default);
    Task<TicketDto?>             GetByReservationDetailAsync(int reservationDetailId,                         CancellationToken cancellationToken = default);
    Task<TicketDto>              CreateAsync(string ticketCode, int reservationDetailId, int ticketStatusId,  CancellationToken cancellationToken = default);
    Task                         ChangeStatusAsync(int id, int ticketStatusId,                               CancellationToken cancellationToken = default);
    Task                         DeleteAsync(int id,                                                         CancellationToken cancellationToken = default);
}

public sealed record TicketDto(
    int      Id,
    string   TicketCode,
    int      ReservationDetailId,
    DateTime IssueDate,
    int      TicketStatusId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/CreateTicketUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateTicketUseCase
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public CreateTicketUseCase(ITicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketAggregate> ExecuteAsync(
        string            ticketCode,
        int               reservationDetailId,
        int               ticketStatusId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // TicketId(1) es placeholder; EF Core asigna el Id real al insertar.
        var ticket = new TicketAggregate(
            new TicketId(1),
            ticketCode,
            reservationDetailId,
            now,
            ticketStatusId,
            now);

        await _repository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return ticket;
    }
}
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/DeleteTicketUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteTicketUseCase
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public DeleteTicketUseCase(ITicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new TicketId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/GetAllTicketsUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;

public sealed class GetAllTicketsUseCase
{
    private readonly ITicketRepository _repository;

    public GetAllTicketsUseCase(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TicketAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/GetTicketByIdUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;

public sealed class GetTicketByIdUseCase
{
    private readonly ITicketRepository _repository;

    public GetTicketByIdUseCase(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<TicketAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new TicketId(id), cancellationToken);
}
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/ChangeTicketStatusUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia el estado del tiquete (ISSUED → USED, ISSUED → CANCELLED, etc.).
/// ticket_code, reservation_detail_id e issue_date son inmutables.
/// </summary>
public sealed class ChangeTicketStatusUseCase
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork       _unitOfWork;

    public ChangeTicketStatusUseCase(ITicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               ticketStatusId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _repository.GetByIdAsync(new TicketId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket with id {id} was not found.");

        ticket.ChangeStatus(ticketStatusId);
        await _repository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
```

---

### RUTA: `src/Modules/Ticket/Application/UseCases/GetTicketByReservationDetailUseCase.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;

/// <summary>
/// Obtiene el tiquete de una línea de reserva.
/// Retorna null si aún no se ha emitido el tiquete para esa línea.
/// La UNIQUE sobre reservation_detail_id garantiza como máximo un resultado.
/// </summary>
public sealed class GetTicketByReservationDetailUseCase
{
    private readonly ITicketRepository _repository;

    public GetTicketByReservationDetailUseCase(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<TicketAggregate?> ExecuteAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByReservationDetailAsync(reservationDetailId, cancellationToken);
}
```

---

### RUTA: `src/Modules/Ticket/Application/Services/TicketService.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;

public sealed class TicketService : ITicketService
{
    private readonly CreateTicketUseCase                  _create;
    private readonly DeleteTicketUseCase                  _delete;
    private readonly GetAllTicketsUseCase                 _getAll;
    private readonly GetTicketByIdUseCase                 _getById;
    private readonly ChangeTicketStatusUseCase            _changeStatus;
    private readonly GetTicketByReservationDetailUseCase  _getByDetail;

    public TicketService(
        CreateTicketUseCase                 create,
        DeleteTicketUseCase                 delete,
        GetAllTicketsUseCase                getAll,
        GetTicketByIdUseCase                getById,
        ChangeTicketStatusUseCase           changeStatus,
        GetTicketByReservationDetailUseCase getByDetail)
    {
        _create       = create;
        _delete       = delete;
        _getAll       = getAll;
        _getById      = getById;
        _changeStatus = changeStatus;
        _getByDetail  = getByDetail;
    }

    public async Task<TicketDto> CreateAsync(
        string            ticketCode,
        int               reservationDetailId,
        int               ticketStatusId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(ticketCode, reservationDetailId, ticketStatusId, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<TicketDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<TicketDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task ChangeStatusAsync(
        int               id,
        int               ticketStatusId,
        CancellationToken cancellationToken = default)
        => await _changeStatus.ExecuteAsync(id, ticketStatusId, cancellationToken);

    public async Task<TicketDto?> GetByReservationDetailAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getByDetail.ExecuteAsync(reservationDetailId, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static TicketDto ToDto(TicketAggregate agg)
        => new(
            agg.Id.Value,
            agg.TicketCode,
            agg.ReservationDetailId,
            agg.IssueDate,
            agg.TicketStatusId,
            agg.CreatedAt,
            agg.UpdatedAt);
}
```

---

## 3. INFRASTRUCTURE

---

### RUTA: `src/Modules/Ticket/Infrastructure/entity/TicketEntity.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Infrastructure.Entity;

public sealed class TicketEntity
{
    public int       Id                  { get; set; }
    public string    TicketCode          { get; set; } = null!;
    public int       ReservationDetailId { get; set; }
    public DateTime  IssueDate           { get; set; }
    public int       TicketStatusId      { get; set; }
    public DateTime  CreatedAt           { get; set; }
    public DateTime? UpdatedAt           { get; set; }
}
```

---

### RUTA: `src/Modules/Ticket/Infrastructure/entity/TicketEntityConfiguration.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class TicketEntityConfiguration : IEntityTypeConfiguration<TicketEntity>
{
    public void Configure(EntityTypeBuilder<TicketEntity> builder)
    {
        builder.ToTable("ticket");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("ticket_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.TicketCode)
               .HasColumnName("ticket_code")
               .IsRequired()
               .HasMaxLength(30);

        builder.HasIndex(e => e.TicketCode)
               .IsUnique()
               .HasDatabaseName("uq_ticket_code");

        builder.Property(e => e.ReservationDetailId)
               .HasColumnName("reservation_detail_id")
               .IsRequired();

        // UNIQUE (reservation_detail_id) — un tiquete por línea de reserva
        builder.HasIndex(e => e.ReservationDetailId)
               .IsUnique()
               .HasDatabaseName("uq_ticket_reservation_detail");

        builder.Property(e => e.IssueDate)
               .HasColumnName("issue_date")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.TicketStatusId)
               .HasColumnName("ticket_status_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasColumnType("timestamp")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);
    }
}
```

---

### RUTA: `src/Modules/Ticket/Infrastructure/repository/TicketRepository.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static TicketAggregate ToDomain(TicketEntity entity)
        => new(
            new TicketId(entity.Id),
            entity.TicketCode,
            entity.ReservationDetailId,
            entity.IssueDate,
            entity.TicketStatusId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<TicketAggregate?> GetByIdAsync(
        TicketId          id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<TicketAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Tickets
            .AsNoTracking()
            .OrderByDescending(e => e.IssueDate)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<TicketAggregate?> GetByReservationDetailAsync(
        int               reservationDetailId,
        CancellationToken cancellationToken = default)
    {
        // reservation_detail_id es UNIQUE — FirstOrDefault es correcto.
        var entity = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ReservationDetailId == reservationDetailId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        TicketAggregate   ticket,
        CancellationToken cancellationToken = default)
    {
        var entity = new TicketEntity
        {
            TicketCode          = ticket.TicketCode,
            ReservationDetailId = ticket.ReservationDetailId,
            IssueDate           = ticket.IssueDate,
            TicketStatusId      = ticket.TicketStatusId,
            CreatedAt           = ticket.CreatedAt,
            UpdatedAt           = ticket.UpdatedAt
        };
        await _context.Tickets.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        TicketAggregate   ticket,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Tickets
            .FirstOrDefaultAsync(e => e.Id == ticket.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketEntity with id {ticket.Id.Value} not found.");

        // Solo TicketStatusId y UpdatedAt son mutables.
        // TicketCode, ReservationDetailId e IssueDate son inmutables.
        entity.TicketStatusId = ticket.TicketStatusId;
        entity.UpdatedAt      = ticket.UpdatedAt;

        _context.Tickets.Update(entity);
    }

    public async Task DeleteAsync(
        TicketId          id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Tickets
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"TicketEntity with id {id.Value} not found.");

        _context.Tickets.Remove(entity);
    }
}
```

---

## 4. UI

---

### RUTA: `src/Modules/Ticket/UI/TicketConsoleUI.cs`

```csharp
namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.UI;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Ticket.Application.Interfaces;

public sealed class TicketConsoleUI
{
    private readonly ITicketService _service;

    public TicketConsoleUI(ITicketService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine("\n========== TICKET MODULE ==========");
            Console.WriteLine("1. List all tickets");
            Console.WriteLine("2. Get ticket by ID");
            Console.WriteLine("3. Get ticket by reservation detail");
            Console.WriteLine("4. Issue ticket");
            Console.WriteLine("5. Change ticket status");
            Console.WriteLine("6. Delete ticket");
            Console.WriteLine("0. Exit");
            Console.Write("Select an option: ");

            var option = Console.ReadLine()?.Trim();

            switch (option)
            {
                case "1": await ListAllAsync();           break;
                case "2": await GetByIdAsync();           break;
                case "3": await GetByDetailAsync();       break;
                case "4": await IssueTicketAsync();       break;
                case "5": await ChangeStatusAsync();      break;
                case "6": await DeleteAsync();            break;
                case "0": running = false;                break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task ListAllAsync()
    {
        var tickets = await _service.GetAllAsync();
        Console.WriteLine("\n--- All Tickets ---");
        foreach (var t in tickets) PrintTicket(t);
    }

    private async Task GetByIdAsync()
    {
        Console.Write("Enter ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByIdAsync(id);
        if (t is null) Console.WriteLine($"Ticket with ID {id} not found.");
        else           PrintTicket(t);
    }

    private async Task GetByDetailAsync()
    {
        Console.Write("Enter Reservation Detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int detailId))
        { Console.WriteLine("Invalid ID."); return; }

        var t = await _service.GetByReservationDetailAsync(detailId);
        if (t is null)
            Console.WriteLine($"No ticket issued for reservation detail {detailId}.");
        else
            PrintTicket(t);
    }

    private async Task IssueTicketAsync()
    {
        Console.Write("Ticket code (max 30 chars): ");
        var code = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        { Console.WriteLine("Code cannot be empty."); return; }

        Console.Write("Reservation Detail ID: ");
        if (!int.TryParse(Console.ReadLine(), out int detailId))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("Initial Ticket Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        try
        {
            var created = await _service.CreateAsync(code, detailId, statusId);
            Console.WriteLine(
                $"Ticket issued: [{created.Id}] {created.TicketCode} | " +
                $"Detail:{created.ReservationDetailId} | Status:{created.TicketStatusId} | " +
                $"Issued:{created.IssueDate:yyyy-MM-dd HH:mm}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private async Task ChangeStatusAsync()
    {
        Console.Write("Ticket ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        Console.Write("New Ticket Status ID: ");
        if (!int.TryParse(Console.ReadLine(), out int statusId))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.ChangeStatusAsync(id, statusId);
        Console.WriteLine("Ticket status changed successfully.");
    }

    private async Task DeleteAsync()
    {
        Console.Write("Ticket ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        { Console.WriteLine("Invalid ID."); return; }

        await _service.DeleteAsync(id);
        Console.WriteLine("Ticket deleted successfully.");
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    private static void PrintTicket(TicketDto t)
        => Console.WriteLine(
            $"  [{t.Id}] {t.TicketCode} | Detail:{t.ReservationDetailId} | " +
            $"Status:{t.TicketStatusId} | Issued:{t.IssueDate:yyyy-MM-dd HH:mm}" +
            (t.UpdatedAt.HasValue ? $" | Updated:{t.UpdatedAt:yyyy-MM-dd HH:mm}" : string.Empty));
}
```

---

## 5. REGISTRO DE DEPENDENCIAS

### RUTA: `src/Program.cs` _(fragmento — agregar en el bloque de servicios)_

```csharp
// ── Ticket Module ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<CreateTicketUseCase>();
builder.Services.AddScoped<DeleteTicketUseCase>();
builder.Services.AddScoped<GetAllTicketsUseCase>();
builder.Services.AddScoped<GetTicketByIdUseCase>();
builder.Services.AddScoped<ChangeTicketStatusUseCase>();
builder.Services.AddScoped<GetTicketByReservationDetailUseCase>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<TicketConsoleUI>();
```

---

## 6. ÁRBOL DE ARCHIVOS

```
src/Modules/Ticket/
├── Application/
│   ├── Interfaces/
│   │   └── ITicketService.cs
│   ├── Services/
│   │   └── TicketService.cs
│   └── UseCases/
│       ├── ChangeTicketStatusUseCase.cs
│       ├── CreateTicketUseCase.cs
│       ├── DeleteTicketUseCase.cs
│       ├── GetAllTicketsUseCase.cs
│       ├── GetTicketByIdUseCase.cs
│       └── GetTicketByReservationDetailUseCase.cs
├── Domain/
│   ├── aggregate/
│   │   └── TicketAggregate.cs
│   ├── Repositories/
│   │   └── ITicketRepository.cs
│   └── valueObject/
│       └── TicketId.cs
├── Infrastructure/
│   ├── entity/
│   │   ├── TicketEntity.cs
│   │   └── TicketEntityConfiguration.cs
│   └── repository/
│       └── TicketRepository.cs
└── UI/
    └── TicketConsoleUI.cs
```

---

_Generado para: Sistema_de_gestion_de_tiquetes_Aereos — Módulo Ticket_  
_Stack: .NET 8 · EF Core 8 · Arquitectura Hexagonal Pura_
