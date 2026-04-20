namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaggageAllowanceRepository : IBaggageAllowanceRepository
{
    private readonly AppDbContext _context;

    public BaggageAllowanceRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaggageAllowanceAggregate ToDomain(BaggageAllowanceEntity entity)
        => new(
            new BaggageAllowanceId(entity.Id),
            entity.CabinClassId,
            entity.FareTypeId,
            entity.CarryOnPieces,
            entity.CarryOnKg,
            entity.CheckedPieces,
            entity.CheckedKg);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaggageAllowanceAggregate?> GetByIdAsync(
        BaggageAllowanceId id,
        CancellationToken  cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaggageAllowanceAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaggageAllowances
            .AsNoTracking()
            .OrderBy(e => e.CabinClassId)
            .ThenBy(e => e.FareTypeId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task<BaggageAllowanceAggregate?> GetByCabinAndFareAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
    {
        // UNIQUE (cabin_class_id, fare_type_id) — FirstOrDefault es correcto.
        var entity = await _context.BaggageAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.CabinClassId == cabinClassId && e.FareTypeId == fareTypeId,
                cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(
        BaggageAllowanceAggregate baggageAllowance,
        CancellationToken         cancellationToken = default)
    {
        var entity = new BaggageAllowanceEntity
        {
            CabinClassId  = baggageAllowance.CabinClassId,
            FareTypeId    = baggageAllowance.FareTypeId,
            CarryOnPieces = baggageAllowance.CarryOnPieces,
            CarryOnKg     = baggageAllowance.CarryOnKg,
            CheckedPieces = baggageAllowance.CheckedPieces,
            CheckedKg     = baggageAllowance.CheckedKg
        };
        await _context.BaggageAllowances.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaggageAllowanceAggregate baggageAllowance,
        CancellationToken         cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .FirstOrDefaultAsync(e => e.Id == baggageAllowance.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageAllowanceEntity with id {baggageAllowance.Id.Value} not found.");

        // Solo los límites de equipaje son mutables.
        // CabinClassId y FareTypeId son la clave de negocio — inmutables.
        entity.CarryOnPieces = baggageAllowance.CarryOnPieces;
        entity.CarryOnKg     = baggageAllowance.CarryOnKg;
        entity.CheckedPieces = baggageAllowance.CheckedPieces;
        entity.CheckedKg     = baggageAllowance.CheckedKg;

        _context.BaggageAllowances.Update(entity);
    }

    public async Task DeleteAsync(
        BaggageAllowanceId id,
        CancellationToken  cancellationToken = default)
    {
        var entity = await _context.BaggageAllowances
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaggageAllowanceEntity with id {id.Value} not found.");

        _context.BaggageAllowances.Remove(entity);
    }
}
