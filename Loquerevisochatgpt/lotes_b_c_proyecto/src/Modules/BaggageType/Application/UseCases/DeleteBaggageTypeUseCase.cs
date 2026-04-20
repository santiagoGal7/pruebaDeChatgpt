namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaggageTypeUseCase
{
    private readonly IBaggageTypeRepository _repository;
    private readonly IUnitOfWork            _unitOfWork;

    public DeleteBaggageTypeUseCase(IBaggageTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaggageTypeId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
