namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteBaseFlightUseCase
{
    private readonly IBaseFlightRepository _repository;
    private readonly IUnitOfWork           _unitOfWork;

    public DeleteBaseFlightUseCase(IBaseFlightRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new BaseFlightId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
