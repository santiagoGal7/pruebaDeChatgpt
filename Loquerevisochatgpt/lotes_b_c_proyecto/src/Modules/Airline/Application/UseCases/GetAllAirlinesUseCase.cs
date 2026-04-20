namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.UseCases;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Application.Interfaces;
public sealed class GetAllAirlinesUseCase
{
    private readonly IAirlineService _service;
    public GetAllAirlinesUseCase(IAirlineService service) => _service = service;
    public Task<IReadOnlyList<AirlineDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _service.GetAllAsync(cancellationToken);
}
