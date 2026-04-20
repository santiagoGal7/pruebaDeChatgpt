namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Application.Interfaces;

public interface IAircraftTypeService
{
    Task<AircraftTypeDto> CreateAsync(CreateAircraftTypeRequest request, CancellationToken cancellationToken = default);
    Task<AircraftTypeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AircraftTypeDto> UpdateAsync(int id, UpdateAircraftTypeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AircraftTypeDto(int AircraftTypeId, int ManufacturerId, string Model, int TotalSeats, decimal CargoCapacityKg);
public sealed record CreateAircraftTypeRequest(int ManufacturerId, string Model, int TotalSeats, decimal CargoCapacityKg = 0);
public sealed record UpdateAircraftTypeRequest(int ManufacturerId, string Model, int TotalSeats, decimal CargoCapacityKg);
