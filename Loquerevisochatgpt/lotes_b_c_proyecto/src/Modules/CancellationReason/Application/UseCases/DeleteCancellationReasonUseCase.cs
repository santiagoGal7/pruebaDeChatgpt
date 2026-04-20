namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCancellationReasonUseCase
{
    private readonly ICancellationReasonRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public DeleteCancellationReasonUseCase(ICancellationReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CancellationReasonId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
