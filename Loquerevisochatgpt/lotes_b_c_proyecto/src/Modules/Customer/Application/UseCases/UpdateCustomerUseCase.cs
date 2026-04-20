namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

/// <summary>
/// Actualiza los datos de contacto del cliente (phone, email).
/// PersonId no es modificable — define a quién identifica el cliente.
/// </summary>
public sealed class UpdateCustomerUseCase
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public UpdateCustomerUseCase(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(new CustomerId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Customer with id {id} was not found.");

        customer.UpdateContact(phone, email);
        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
