namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Domain.ValueObject;

/// <summary>Puerto de salida: contrato de persistencia para el módulo Airport.</summary>
public interface IAirportRepository
{
    Task<AirportAggregate?> GetByIdAsync(AirportId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AirportAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AirportAggregate airport, CancellationToken cancellationToken = default);
    void Update(AirportAggregate airport);
    void Delete(AirportAggregate airport);
}
