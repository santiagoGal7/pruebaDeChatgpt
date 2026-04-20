namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        var delayReason = await _repository.GetByIdAsync(new DelayReasonId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"DelayReason with id {id} was not found.");

        delayReason.Update(name, category);
        await _repository.UpdateAsync(delayReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
