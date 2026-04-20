namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;

public sealed class GetCheckInStatusByIdUseCase
{
    private readonly ICheckInStatusRepository _repository;

    public GetCheckInStatusByIdUseCase(ICheckInStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInStatusAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CheckInStatusId(id), cancellationToken);
}
