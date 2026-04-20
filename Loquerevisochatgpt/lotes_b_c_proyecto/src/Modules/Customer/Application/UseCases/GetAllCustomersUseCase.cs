namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;

public sealed class GetAllCustomersUseCase
{
    private readonly ICustomerRepository _repository;

    public GetAllCustomersUseCase(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CustomerAggregate>> ExecuteAsync(
        CancellationToken cancellationToken = default)
        => await _repository.GetAllAsync(cancellationToken);
}
