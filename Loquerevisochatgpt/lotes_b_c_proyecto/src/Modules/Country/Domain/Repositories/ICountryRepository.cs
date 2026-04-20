namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;

public interface ICountryRepository
{
    Task<CountryAggregate?>             GetByIdAsync(CountryId id,           CancellationToken cancellationToken = default);
    Task<IEnumerable<CountryAggregate>> GetAllAsync(                         CancellationToken cancellationToken = default);
    Task                                AddAsync(CountryAggregate country,   CancellationToken cancellationToken = default);
    Task                                UpdateAsync(CountryAggregate country, CancellationToken cancellationToken = default);
    Task                                DeleteAsync(CountryId id,            CancellationToken cancellationToken = default);
}
