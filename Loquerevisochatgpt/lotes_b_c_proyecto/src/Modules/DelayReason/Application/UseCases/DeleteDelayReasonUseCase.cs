namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new DelayReasonId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
