namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;

public sealed class GetAllBaseFlightsUseCase
{
    private readonly IBaseFlightRepository _repository;

    public GetAllBaseFlightsUseCase(IBaseFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaseFlightAggregate>> ExecuteAsync(CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
