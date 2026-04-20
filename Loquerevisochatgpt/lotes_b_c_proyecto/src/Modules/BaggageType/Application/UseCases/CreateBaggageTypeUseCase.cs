namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaggageTypeAggregate> ExecuteAsync(
        string            name,
        decimal           maxWeightKg,
        decimal           extraFee,
        CancellationToken cancellationToken = default)
    {
        // BaggageTypeId(1) es placeholder; EF Core asigna el Id real al insertar.
        var baggageType = new BaggageTypeAggregate(
            new BaggageTypeId(1), name, maxWeightKg, extraFee);

        await _repository.AddAsync(baggageType, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baggageType;
    }
}
