namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>Caso de uso: Actualizar ciudad.</summary>
public sealed class UpdateCityUseCase
{
    private readonly ICityService _service;
    public UpdateCityUseCase(ICityService service) => _service = service;

    public Task<CityDto> ExecuteAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default)
        => _service.UpdateAsync(id, request, cancellationToken);
}
