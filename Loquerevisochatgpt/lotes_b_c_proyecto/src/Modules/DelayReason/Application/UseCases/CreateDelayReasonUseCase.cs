namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.DelayReason.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateDelayReasonUseCase
{
    private readonly IDelayReasonRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateDelayReasonUseCase(IDelayReasonRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DelayReasonAggregate> ExecuteAsync(
        string            name,
        string            category,
        CancellationToken cancellationToken = default)
    {
        // DelayReasonId(1) es placeholder; EF Core asigna el Id real al insertar.
        var delayReason = new DelayReasonAggregate(new DelayReasonId(1), name, category);

        await _repository.AddAsync(delayReason, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return delayReason;
    }
}
