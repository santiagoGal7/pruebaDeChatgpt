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
