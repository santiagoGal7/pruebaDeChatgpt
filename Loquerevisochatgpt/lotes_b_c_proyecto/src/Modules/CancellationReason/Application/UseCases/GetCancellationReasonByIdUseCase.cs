namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;

public sealed class GetCancellationReasonByIdUseCase
{
    private readonly ICancellationReasonRepository _repository;

    public GetCancellationReasonByIdUseCase(ICancellationReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<CancellationReasonAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CancellationReasonId(id), cancellationToken);
}
