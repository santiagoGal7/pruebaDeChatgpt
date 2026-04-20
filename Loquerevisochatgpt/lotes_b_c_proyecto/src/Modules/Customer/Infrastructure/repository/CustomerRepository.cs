namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Repository;

using Microsoft.EntityFrameworkCore;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Context;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    // ── Mapeos privados ───────────────────────────────────────────────────────

    private static CustomerAggregate ToDomain(CustomerEntity entity)
        => new(
            new CustomerId(entity.Id),
            entity.PersonId,
            entity.Phone,
            entity.Email,
            entity.CreatedAt,
            entity.UpdatedAt);

    // ── Operaciones ───────────────────────────────────────────────────────────

    public async Task<CustomerAggregate?> GetByIdAsync(
        CustomerId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IEnumerable<CustomerAggregate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Customers
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain);
    }

    public async Task AddAsync(
        CustomerAggregate customer,
        CancellationToken cancellationToken = default)
    {
        var entity = new CustomerEntity
        {
            PersonId  = customer.PersonId,
            Phone     = customer.Phone,
            Email     = customer.Email,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
        await _context.Customers.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        CustomerAggregate customer,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .FirstOrDefaultAsync(e => e.Id == customer.Id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CustomerEntity with id {customer.Id.Value} not found.");

        // PersonId no se modifica — es la clave de negocio.
        entity.Phone     = customer.Phone;
        entity.Email     = customer.Email;
        entity.UpdatedAt = customer.UpdatedAt;

        _context.Customers.Update(entity);
    }

    public async Task DeleteAsync(
        CustomerId        id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Customers
            .FirstOrDefaultAsync(e => e.Id == id.Value, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"CustomerEntity with id {id.Value} not found.");

        _context.Customers.Remove(entity);
    }
}
