namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>Caso de uso: Obtener todas las ciudades.</summary>
public sealed class GetAllCitiesUseCase
{
    private readonly ICityService _service;
    public GetAllCitiesUseCase(ICityService service) => _service = service;

    public Task<IReadOnlyList<CityDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _service.GetAllAsync(cancellationToken);
}
