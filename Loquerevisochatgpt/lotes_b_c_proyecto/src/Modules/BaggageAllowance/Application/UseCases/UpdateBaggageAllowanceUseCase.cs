namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza los límites de franquicia.
/// cabin_class_id y fare_type_id son la clave de negocio — inmutables.
/// </summary>
public sealed class UpdateBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public UpdateBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        int               carryOnPieces,
        decimal           carryOnKg,
        int               checkedPieces,
        decimal           checkedKg,
        CancellationToken cancellationToken = default)
    {
        var baggageAllowance = await _repository.GetByIdAsync(new BaggageAllowanceId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"BaggageAllowance with id {id} was not found.");

        baggageAllowance.Update(carryOnPieces, carryOnKg, checkedPieces, checkedKg);
        await _repository.UpdateAsync(baggageAllowance, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
