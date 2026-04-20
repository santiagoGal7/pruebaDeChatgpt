namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;

public sealed class GetBaggageAllowanceByIdUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetBaggageAllowanceByIdUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageAllowanceAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new BaggageAllowanceId(id), cancellationToken);
}
