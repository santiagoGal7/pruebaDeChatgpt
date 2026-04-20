namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CancellationReasonRepository : ICancellationReasonRepository
{
    private readonly AppDbContext _context;

    public CancellationReasonRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CancellationReasonAggregate ToDomain(CancellationReasonEntity entity)
        => new(new CancellationReasonId(entity.Id), entity.Name);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CancellationReasonAggregate?> GetByIdAsync(
        CancellationReasonId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CancellationReasonAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.CancellationReasons
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CancellationReasonAggregate cancellationReason,
        CancellationToken           cancellationToken = default)
    {
        var entity = new CancellationReasonEntity { Name = cancellationReason.Name };
        await _context.CancellationReasons.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CancellationReasonAggregate cancellationReason,
        CancellationToken           cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .FirstOrDefaultAsync(e => e.Id == cancellationReason.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CancellationReasonEntity with id {cancellationReason.Id.Value} not found.");

        entity.Name = cancellationReason.Name;
        _context.CancellationReasons.Update(entity);
    }

    public async Task DeleteAsync(
        CancellationReasonId id,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.CancellationReasons
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CancellationReasonEntity with id {id.Value} not found.");

        _context.CancellationReasons.Remove(entity);
    }
}
