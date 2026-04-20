namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public CreateCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInStatusAggregate> ExecuteAsync(
        string            name,
        CancellationToken cancellationToken = default)
    {
        // CheckInStatusId(1) es placeholder; EF Core asigna el Id real al insertar.
        var checkInStatus = new CheckInStatusAggregate(new CheckInStatusId(1), name);

        await _repository.AddAsync(checkInStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return checkInStatus;
    }
}
