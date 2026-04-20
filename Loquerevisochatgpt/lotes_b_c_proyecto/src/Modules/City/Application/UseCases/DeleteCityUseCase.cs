namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Application.Interfaces;

/// <summary>Caso de uso: Eliminar ciudad.</summary>
public sealed class DeleteCityUseCase
{
    private readonly ICityService _service;
    public DeleteCityUseCase(ICityService service) => _service = service;

    public Task ExecuteAsync(int id, CancellationToken cancellationToken = default)
        => _service.DeleteAsync(id, cancellationToken);
}
