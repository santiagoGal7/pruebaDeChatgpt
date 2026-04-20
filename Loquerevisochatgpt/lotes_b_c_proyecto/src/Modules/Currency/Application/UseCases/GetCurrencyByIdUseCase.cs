namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;

public sealed class GetCurrencyByIdUseCase
{
    private readonly ICurrencyRepository _repository;

    public GetCurrencyByIdUseCase(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CurrencyAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CurrencyId(id), cancellationToken);
}
