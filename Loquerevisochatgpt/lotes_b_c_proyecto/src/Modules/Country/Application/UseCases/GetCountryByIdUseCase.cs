namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;

public sealed class GetCountryByIdUseCase
{
    private readonly ICountryRepository _repository;

    public GetCountryByIdUseCase(ICountryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CountryAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CountryId(id), cancellationToken);
}
