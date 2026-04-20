namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto?>             GetByIdAsync(int id,                                        CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> GetAllAsync(                                                CancellationToken cancellationToken = default);
    Task<CustomerDto>              CreateAsync(int personId, string? phone, string? email,     CancellationToken cancellationToken = default);
    Task                           UpdateAsync(int id, string? phone, string? email,           CancellationToken cancellationToken = default);
    Task                           DeleteAsync(int id,                                         CancellationToken cancellationToken = default);
}

public sealed record CustomerDto(
    int      Id,
    int      PersonId,
    string?  Phone,
    string?  Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
