namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Repositories;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Domain.ValueObject;

public interface IAircraftManufacturerRepository
{
    Task<AircraftManufacturerAggregate?> GetByIdAsync(AircraftManufacturerId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftManufacturerAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AircraftManufacturerAggregate manufacturer, CancellationToken cancellationToken = default);
    void Update(AircraftManufacturerAggregate manufacturer);
    void Delete(AircraftManufacturerAggregate manufacturer);
}
