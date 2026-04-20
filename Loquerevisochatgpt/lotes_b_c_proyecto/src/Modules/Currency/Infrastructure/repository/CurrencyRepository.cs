namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _context;

    public CurrencyRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CurrencyAggregate ToDomain(CurrencyEntity entity)
        => new(new CurrencyId(entity.Id), entity.IsoCode, entity.Name, entity.Symbol);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CurrencyAggregate?> GetByIdAsync(
        CurrencyId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CurrencyAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Currencies
            .AsNoTracking()
            .OrderBy(e => e.IsoCode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CurrencyAggregate currency,
        CancellationToken cancellationToken = default)
    {
        var entity = new CurrencyEntity
        {
            IsoCode = currency.IsoCode,
            Name    = currency.Name,
            Symbol  = currency.Symbol
        };
        await _context.Currencies.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CurrencyAggregate currency,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .FirstOrDefaultAsync(e => e.Id == currency.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CurrencyEntity with id {currency.Id.Value} not found.");

        entity.IsoCode = currency.IsoCode;
        entity.Name    = currency.Name;
        entity.Symbol  = currency.Symbol;

        _context.Currencies.Update(entity);
    }

    public async Task DeleteAsync(
        CurrencyId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Currencies
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CurrencyEntity with id {id.Value} not found.");

        _context.Currencies.Remove(entity);
    }
}
