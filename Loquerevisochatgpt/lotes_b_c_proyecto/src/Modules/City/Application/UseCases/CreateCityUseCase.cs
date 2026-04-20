namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>Caso de uso: Crear ciudad.</summary>
public sealed class CreateCityUseCase
{
    private readonly ICityService _service;
    public CreateCityUseCase(ICityService service) => _service = service;

    public Task<CityDto> ExecuteAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
        => _service.CreateAsync(request, cancellationToken);
}
