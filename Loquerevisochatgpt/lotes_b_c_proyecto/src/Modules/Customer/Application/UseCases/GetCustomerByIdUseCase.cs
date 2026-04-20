namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public sealed class GetCustomerByIdUseCase
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdUseCase(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerAggregate?> ExecuteAsync(
        int               id,
        CancellationToken cancellationToken = default)
        => await _repository.GetByIdAsync(new CustomerId(id), cancellationToken);
}
