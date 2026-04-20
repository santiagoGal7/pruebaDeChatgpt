namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Application.Interfaces;

/// <summary>Puerto de entrada: contrato del servicio de aplicación para Airport.</summary>
public interface IAirportService
{
    Task<AirportDto> CreateAsync(CreateAirportRequest request, CancellationToken cancellationToken = default);
    Task<AirportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AirportDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AirportDto> UpdateAsync(int id, UpdateAirportRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AirportDto(int AirportId, string IataCode, string Name, int CityId, DateTime CreatedAt);
public sealed record CreateAirportRequest(string IataCode, string Name, int CityId);
public sealed record UpdateAirportRequest(string IataCode, string Name, int CityId);
