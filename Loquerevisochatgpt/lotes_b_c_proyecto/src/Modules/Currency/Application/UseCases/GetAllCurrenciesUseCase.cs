namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;

public sealed class GetAllCurrenciesUseCase
{
    private readonly ICurrencyRepository _repository;

    public GetAllCurrenciesUseCase(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CurrencyAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
