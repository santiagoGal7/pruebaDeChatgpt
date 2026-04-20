namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;

public sealed class GetAllCountriesUseCase
{
    private readonly ICountryRepository _repository;

    public GetAllCountriesUseCase(ICountryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CountryAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
