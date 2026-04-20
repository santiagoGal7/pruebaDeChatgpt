namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Cambia el estado del check-in (PENDING → CHECKED_IN → BOARDED, etc.)
/// y opcionalmente actualiza el número de mostrador.
/// ticket_id y check_in_time son inmutables.
/// </summary>
public sealed class ChangeCheckInStatusUseCase
{
    private readonly ICheckInRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public ChangeCheckInStatusUseCase(ICheckInRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        var checkIn = await _repository.GetByIdAsync(new CheckInId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CheckIn with id {id} was not found.");

        checkIn.ChangeStatus(checkInStatusId, counterNumber);
        await _repository.UpdateAsync(checkIn, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
