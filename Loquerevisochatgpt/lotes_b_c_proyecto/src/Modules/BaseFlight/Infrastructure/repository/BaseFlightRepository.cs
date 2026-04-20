namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class BaseFlightRepository : IBaseFlightRepository
{
    private readonly AppDbContext _context;

    public BaseFlightRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static BaseFlightAggregate ToDomain(BaseFlightEntity entity)
        => new(
            new BaseFlightId(entity.Id),
            entity.FlightCode,
            entity.AirlineId,
            entity.RouteId,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<BaseFlightAggregate?> GetByIdAsync(
        BaseFlightId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<BaseFlightAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.BaseFlights
            .AsNoTracking()
            .OrderBy(e => e.FlightCode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        BaseFlightAggregate baseFlight,
        CancellationToken   cancellationToken = default)
    {
        // Id lo asigna la BD (AUTO_INCREMENT); no se incluye en la entidad al insertar.
        var entity = new BaseFlightEntity
        {
            FlightCode = baseFlight.FlightCode,
            AirlineId  = baseFlight.AirlineId,
            RouteId    = baseFlight.RouteId,
            CreatedAt  = baseFlight.CreatedAt,
            UpdatedAt  = baseFlight.UpdatedAt
        };
        await _context.BaseFlights.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        BaseFlightAggregate baseFlight,
        CancellationToken   cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .FirstOrDefaultAsync(e => e.Id == baseFlight.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaseFlightEntity with id {baseFlight.Id.Value} not found.");

        entity.FlightCode = baseFlight.FlightCode;
        entity.AirlineId  = baseFlight.AirlineId;
        entity.RouteId    = baseFlight.RouteId;
        entity.UpdatedAt  = baseFlight.UpdatedAt;

        _context.BaseFlights.Update(entity);
    }

    public async Task DeleteAsync(
        BaseFlightId      id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.BaseFlights
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"BaseFlightEntity with id {id.Value} not found.");

        _context.BaseFlights.Remove(entity);
    }
}
