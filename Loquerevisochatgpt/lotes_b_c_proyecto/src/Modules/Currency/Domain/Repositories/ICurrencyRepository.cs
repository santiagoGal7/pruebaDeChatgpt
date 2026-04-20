namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public interface ICurrencyRepository
{
    Task<CurrencyAggregate?>             GetByIdAsync(CurrencyId id,              CancellationToken cancellationToken = default);
    Task<IEnumerable<CurrencyAggregate>> GetAllAsync(                             CancellationToken cancellationToken = default);
    Task                                 AddAsync(CurrencyAggregate currency,     CancellationToken cancellationToken = default);
    Task                                 UpdateAsync(CurrencyAggregate currency,  CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(CurrencyId id,               CancellationToken cancellationToken = default);
}
