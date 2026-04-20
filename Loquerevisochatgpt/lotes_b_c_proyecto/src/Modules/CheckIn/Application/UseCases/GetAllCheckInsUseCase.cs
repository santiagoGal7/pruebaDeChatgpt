namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

public sealed class GetAllCheckInsUseCase
{
    private readonly ICheckInRepository _repository;

    public GetAllCheckInsUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CheckInAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
