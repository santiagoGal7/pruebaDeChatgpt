namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;

public sealed class GetBaseFlightByIdUseCase
{
    private readonly IBaseFlightRepository _repository;

    public GetBaseFlightByIdUseCase(IBaseFlightRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseFlightAggregate?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaseFlightId(id), cancellationToken);
}
