namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;

public interface IAircraftService
{
    Task<AircraftDto> CreateAsync(CreateAircraftRequest request, CancellationToken cancellationToken = default);
    Task<AircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AircraftDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AircraftDto> UpdateAsync(int id, UpdateAircraftRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AircraftDto(int AircraftId, int AirlineId, int AircraftTypeId, string RegistrationNumber, int ManufactureYear, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record CreateAircraftRequest(int AirlineId, int AircraftTypeId, string RegistrationNumber, int ManufactureYear, bool IsActive = true);
public sealed record UpdateAircraftRequest(int AirlineId, int AircraftTypeId, string RegistrationNumber, int ManufactureYear, bool IsActive);
