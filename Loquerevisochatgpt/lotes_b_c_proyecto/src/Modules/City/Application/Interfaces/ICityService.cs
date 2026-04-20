namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>
/// Puerto de entrada: contrato del servicio de aplicación para City.
/// </summary>
public interface ICityService
{
    Task<CityDto> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default);
    Task<CityDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CityDto> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record CityDto(int CityId, string Name, int CountryId, DateTime CreatedAt);
public sealed record CreateCityRequest(string Name, int CountryId);
public sealed record UpdateCityRequest(string Name, int CountryId);
