namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public CreateBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseFlightAggregate> ExecuteAsync(
        string flightCode,
        int    airlineId,
        int    routeId,
        CancellationToken cancellationToken = default)
    {
        // BaseFlightId(1) es placeholder; EF Core asigna el Id real al insertar (ValueGeneratedOnAdd).
        var baseFlight = new BaseFlightAggregate(
            new BaseFlightId(1),
            flightCode,
            airlineId,
            routeId,
            DateTime.UtcNow);

        await _repository.AddAsync(baseFlight, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baseFlight;
    }
}
