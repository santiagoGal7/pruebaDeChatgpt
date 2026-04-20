namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        var baggageType = await _repository.GetByIdAsync(new BaggageTypeId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaggageType with id {id} was not found.");

        baggageType.Update(name, maxWeightKg, extraFee);
        await _repository.UpdateAsync(baggageType, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
