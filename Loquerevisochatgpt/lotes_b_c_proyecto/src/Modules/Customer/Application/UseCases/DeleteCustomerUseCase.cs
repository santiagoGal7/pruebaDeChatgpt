namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class DeleteCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public DeleteCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(new CustomerId(id), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
