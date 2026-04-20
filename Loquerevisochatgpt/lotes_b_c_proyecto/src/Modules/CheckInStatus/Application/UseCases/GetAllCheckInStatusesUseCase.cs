namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;

public sealed class GetAllCheckInStatusesUseCase
{
    private readonly ICheckInStatusRepository _repository;

    public GetAllCheckInStatusesUseCase(ICheckInStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CheckInStatusAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
