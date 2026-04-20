namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

public sealed class GetAllBaggageAllowancesUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetAllBaggageAllowancesUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BaggageAllowanceAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
