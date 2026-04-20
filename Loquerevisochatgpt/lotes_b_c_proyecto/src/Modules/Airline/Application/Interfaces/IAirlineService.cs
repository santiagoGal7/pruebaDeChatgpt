namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;

public interface IAirlineService
{
    Task<AirlineDto> CreateAsync(CreateAirlineRequest request, CancellationToken cancellationToken = default);
    Task<AirlineDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AirlineDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AirlineDto> UpdateAsync(int id, UpdateAirlineRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AirlineDto(int AirlineId, string IataCode, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record CreateAirlineRequest(string IataCode, string Name, bool IsActive = true);
public sealed record UpdateAirlineRequest(string IataCode, string Name, bool IsActive);
