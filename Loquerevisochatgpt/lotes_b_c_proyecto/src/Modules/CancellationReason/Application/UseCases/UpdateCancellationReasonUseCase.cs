namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCancellationReasonUseCase
{
    private readonly ICancellationReasonRepository _repository;
    private readonly IUnitOfWork                   _unitOfWork;

    public UpdateCancellationReasonUseCase(ICancellationReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var cancellationReason = await _repository.GetByIdAsync(new CancellationReasonId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"CancellationReason with id {id} was not found.");

        cancellationReason.UpdateName(newName);
        await _repository.UpdateAsync(cancellationReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
