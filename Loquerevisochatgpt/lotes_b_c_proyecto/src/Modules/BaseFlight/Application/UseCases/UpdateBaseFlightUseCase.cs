namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public UpdateBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int    id,
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        var baseFlight = await _repository.GetByIdAsync(new BaseFlightId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaseFlight with id {id} was not found.");

        baseFlight.Update(flightCode, airlineId, routeId);
        await _repository.UpdateAsync(baseFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
