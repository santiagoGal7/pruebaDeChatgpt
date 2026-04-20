namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;

public sealed class GetCheckInByIdUseCase
{
    private readonly ICheckInRepository _repository;

    public GetCheckInByIdUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CheckInId(id), cancellationToken);
}
