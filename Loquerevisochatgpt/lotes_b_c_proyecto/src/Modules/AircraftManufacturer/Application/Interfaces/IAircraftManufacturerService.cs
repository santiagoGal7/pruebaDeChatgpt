namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Application.Interfaces;

public interface IAircraftManufacturerService
{
    Task<AircraftManufacturerDto> CreateAsync(CreateAircraftManufacturerRequest request, CancellationToken cancellationToken = default);
    Task<AircraftManufacturerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftManufacturerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AircraftManufacturerDto> UpdateAsync(int id, UpdateAircraftManufacturerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AircraftManufacturerDto(int ManufacturerId, string Name, int CountryId);
public sealed record CreateAircraftManufacturerRequest(string Name, int CountryId);
public sealed record UpdateAircraftManufacturerRequest(string Name, int CountryId);
