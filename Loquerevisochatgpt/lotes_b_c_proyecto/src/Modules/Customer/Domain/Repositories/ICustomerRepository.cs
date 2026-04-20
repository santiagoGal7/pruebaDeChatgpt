namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;

public interface ICustomerRepository
{
    Task<CustomerAggregate?>             GetByIdAsync(CustomerId id,               CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAggregate>> GetAllAsync(                              CancellationToken cancellationToken = default);
    Task                                 AddAsync(CustomerAggregate customer,      CancellationToken cancellationToken = default);
    Task                                 UpdateAsync(CustomerAggregate customer,   CancellationToken cancellationToken = default);
    Task                                 DeleteAsync(CustomerId id,                CancellationToken cancellationToken = default);
}
