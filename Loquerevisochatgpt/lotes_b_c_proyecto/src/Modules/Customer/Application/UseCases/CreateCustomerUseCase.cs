namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public CreateCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerAggregate> ExecuteAsync(
        int               personId,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        // CustomerId(1) es placeholder; EF Core asigna el Id real al insertar.
        var customer = new CustomerAggregate(
            new CustomerId(1),
            personId,
            phone,
            email,
            DateTime.UtcNow);

        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return customer;
    }
}
