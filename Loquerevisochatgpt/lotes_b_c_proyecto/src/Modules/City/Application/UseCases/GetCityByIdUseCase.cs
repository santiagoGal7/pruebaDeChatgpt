namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>Caso de uso: Obtener ciudad por ID.</summary>
public sealed class GetCityByIdUseCase
{
    private readonly ICityService _service;
    public GetCityByIdUseCase(ICityService service) => _service = service;

    public Task<CityDto?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.GetByIdAsync(id, cancellationToken);
}
