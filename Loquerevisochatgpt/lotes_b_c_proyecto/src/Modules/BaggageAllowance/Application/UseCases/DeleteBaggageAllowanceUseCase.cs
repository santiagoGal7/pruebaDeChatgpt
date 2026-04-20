namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaggageAllowanceUseCase
{
    private readonly IBaggageAllowanceRepository _repository;
    private readonly IUnitOfWork                 _unitOfWork;

    public DeleteBaggageAllowanceUseCase(IBaggageAllowanceRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaggageAllowanceId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
