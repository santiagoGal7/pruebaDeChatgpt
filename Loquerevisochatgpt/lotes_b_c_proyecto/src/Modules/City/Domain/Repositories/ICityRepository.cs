namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Domain.ValueObject;

/// <summary>
/// Puerto de salida: contrato de persistencia para el módulo City.
/// </summary>
public interface ICityRepository
{
    Task<CityAggregate?> GetByIdAsync(CityId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CityAggregate city, CancellationToken cancellationToken = default);
    void Update(CityAggregate city);
    void Delete(CityAggregate city);
}
