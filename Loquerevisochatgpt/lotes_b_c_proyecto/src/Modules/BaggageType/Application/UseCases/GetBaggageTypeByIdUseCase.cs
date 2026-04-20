namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;

public sealed class GetBaggageTypeByIdUseCase
{
    private readonly IBaggageTypeRepository _repository;

    public GetBaggageTypeByIdUseCase(IBaggageTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageTypeAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaggageTypeId(id), cancellationToken);
}
