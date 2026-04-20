namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CheckInStatusRepository : ICheckInStatusRepository
{
    private readonly AppDbContext _context;

    public CheckInStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    private static CheckInStatusAggregate ToDomain(CheckInStatusEntity entity)
        => new(new CheckInStatusId(entity.Id), entity.Name);

    public async Task<CheckInStatusAggregate?> GetByIdAsync(
        CheckInStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CheckInStatusAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.CheckInStatuses
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CheckInStatusAggregate checkInStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = new CheckInStatusEntity { Name = checkInStatus.Name };
        await _context.CheckInStatuses.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CheckInStatusAggregate checkInStatus,
        CancellationToken      cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .FirstOrDefaultAsync(e => e.Id == checkInStatus.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInStatusEntity with id {checkInStatus.Id.Value} not found.");

        entity.Name = checkInStatus.Name;
        _context.CheckInStatuses.Update(entity);
    }

    public async Task DeleteAsync(
        CheckInStatusId   id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.CheckInStatuses
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CheckInStatusEntity with id {id.Value} not found.");

        _context.CheckInStatuses.Remove(entity);
    }
}
