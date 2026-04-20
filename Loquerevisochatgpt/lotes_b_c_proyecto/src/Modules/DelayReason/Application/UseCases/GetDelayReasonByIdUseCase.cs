namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;

public sealed class GetDelayReasonByIdUseCase
{
    private readonly IDelayReasonRepository _repository;

    public GetDelayReasonByIdUseCase(IDelayReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<DelayReasonAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new DelayReasonId(id), cancellationToken);
}
