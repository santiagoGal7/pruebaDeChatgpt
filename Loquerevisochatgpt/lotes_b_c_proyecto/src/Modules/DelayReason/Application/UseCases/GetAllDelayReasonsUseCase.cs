namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;

public sealed class GetAllDelayReasonsUseCase
{
    private readonly IDelayReasonRepository _repository;

    public GetAllDelayReasonsUseCase(IDelayReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<DelayReasonAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
