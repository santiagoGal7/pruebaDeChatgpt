namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaggageTypeRepository : IBaggageTypeRepository
{
    private readonly AppDbContext _context;

    public BaggageTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaggageTypeAggregate ToDomain(BaggageTypeEntity entity)
        => new(new BaggageTypeId(entity.Id), entity.Name, entity.MaxWeightKg, entity.ExtraFee);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaggageTypeAggregate?> GetByIdAsync(
        BaggageTypeId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaggageTypeAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaggageTypes
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        BaggageTypeAggregate baggageType,
        CancellationToken    cancellationToken = default)
    {
        var entity = new BaggageTypeEntity
        {
            Name        = baggageType.Name,
            MaxWeightKg = baggageType.MaxWeightKg,
            ExtraFee    = baggageType.ExtraFee
        };
        await _context.BaggageTypes.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaggageTypeAggregate baggageType,
        CancellationToken    cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .FirstOrDefaultAsync(e => e.Id == baggageType.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageTypeEntity with id {baggageType.Id.Value} not found.");

        entity.Name        = baggageType.Name;
        entity.MaxWeightKg = baggageType.MaxWeightKg;
        entity.ExtraFee    = baggageType.ExtraFee;

        _context.BaggageTypes.Update(entity);
    }

    public async Task DeleteAsync(
        BaggageTypeId     id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaggageTypes
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageTypeEntity with id {id.Value} not found.");

        _context.BaggageTypes.Remove(entity);
    }
}
