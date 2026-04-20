namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;

public sealed class CustomerService : ICustomerService
{
    private readonly CreateCustomerUseCase   _create;
    private readonly DeleteCustomerUseCase   _delete;
    private readonly GetAllCustomersUseCase  _getAll;
    private readonly GetCustomerByIdUseCase  _getById;
    private readonly UpdateCustomerUseCase   _update;

    public CustomerService(
        CreateCustomerUseCase  create,
        DeleteCustomerUseCase  delete,
        GetAllCustomersUseCase getAll,
        GetCustomerByIdUseCase getById,
        UpdateCustomerUseCase  update)
    {
        _create  = create;
        _delete  = delete;
        _getAll  = getAll;
        _getById = getById;
        _update  = update;
    }

    public async Task<CustomerDto> CreateAsync(
        int               personId,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
    {
        var agg = await _create.ExecuteAsync(personId, phone, email, cancellationToken);
        return ToDto(agg);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => await _delete.ExecuteAsync(id, cancellationToken);

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var list = await _getAll.ExecuteAsync(cancellationToken);
        return list.Select(ToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(
        int               id,
        CancellationToken cancellationToken = default)
    {
        var agg = await _getById.ExecuteAsync(id, cancellationToken);
        return agg is null ? null : ToDto(agg);
    }

    public async Task UpdateAsync(
        int               id,
        string?           phone,
        string?           email,
        CancellationToken cancellationToken = default)
        => await _update.ExecuteAsync(id, phone, email, cancellationToken);

    // ── Mapper privado ────────────────────────────────────────────────────────

    private static CustomerDto ToDto(CustomerAggregate agg)
        => new(agg.Id.Value, agg.PersonId, agg.Phone, agg.Email, agg.CreatedAt, agg.UpdatedAt);
}
