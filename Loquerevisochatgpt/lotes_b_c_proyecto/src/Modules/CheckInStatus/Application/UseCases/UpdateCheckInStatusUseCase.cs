namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public UpdateCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var checkInStatus = await _repository.GetByIdAsync(new CheckInStatusId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CheckInStatus with id {id} was not found.");

        checkInStatus.UpdateName(newName);
        await _repository.UpdateAsync(checkInStatus, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
