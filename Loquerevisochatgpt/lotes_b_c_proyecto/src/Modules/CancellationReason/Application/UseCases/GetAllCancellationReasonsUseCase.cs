namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;

public sealed class GetAllCancellationReasonsUseCase
{
    private readonly ICancellationReasonRepository _repository;

    public GetAllCancellationReasonsUseCase(ICancellationReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CancellationReasonAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
