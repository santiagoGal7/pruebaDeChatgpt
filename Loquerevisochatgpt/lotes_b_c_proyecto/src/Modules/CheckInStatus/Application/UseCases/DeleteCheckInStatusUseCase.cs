namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCheckInStatusUseCase
{
    private readonly ICheckInStatusRepository _repository;
    private readonly IUnitOfWork              _unitOfWork;

    public DeleteCheckInStatusUseCase(ICheckInStatusRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CheckInStatusId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
