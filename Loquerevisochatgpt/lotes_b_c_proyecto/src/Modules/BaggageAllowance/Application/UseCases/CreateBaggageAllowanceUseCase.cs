namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public CreateBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaggageAllowanceAggregate> ExecuteAsync(
        int               cabinClassId,
        int               fareTypeId,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        // BaggageAllowanceId(1) es placeholder; EF Core asigna el Id real al insertar.
        var baggageAllowance = new BaggageAllowanceAggregate(
            new BaggageAllowanceId(1),
            cabinClassId, fareTypeId,
            carryOnPieces, carryOnKg,
            checkedPieces, checkedKg);

        await _repository.AddAsync(baggageAllowance, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return baggageAllowance;
    }
}
