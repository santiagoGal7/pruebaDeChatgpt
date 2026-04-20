namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CountryRepository : ICountryRepository
{
    private readonly AppDbContext _context;

    public CountryRepository(AppDbContext context)
    {
        _context = context;
    }

    private static CountryAggregate ToDomain(CountryEntity entity)
        => new(new CountryId(entity.Id), entity.Name);

    public async Task<CountryAggregate?> GetByIdAsync(
        CountryId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CountryAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Countries
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CountryAggregate  country,
        CancellationToken cancellationToken = default)
    {
        var entity = new CountryEntity { Name = country.Name };
        await _context.Countries.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CountryAggregate  country,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Countries
            .FirstOrDefaultAsync(e => e.Id == country.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CountryEntity with id {country.Id.Value} not found.");

        entity.Name = country.Name;
        _context.Countries.Update(entity);
    }

    public async Task DeleteAsync(
        CountryId         id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Countries
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CountryEntity with id {id.Value} not found.");

        _context.Countries.Remove(entity);
    }
}
