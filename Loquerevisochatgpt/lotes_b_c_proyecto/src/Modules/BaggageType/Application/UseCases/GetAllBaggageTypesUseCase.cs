namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;

public sealed class GetAllBaggageTypesUseCase
{
    private readonly IBaggageTypeRepository _repository;

    public GetAllBaggageTypesUseCase(IBaggageTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaggageTypeAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
