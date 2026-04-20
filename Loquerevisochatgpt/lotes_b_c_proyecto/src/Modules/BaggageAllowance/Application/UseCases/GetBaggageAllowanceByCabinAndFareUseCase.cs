namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Domain.Repositories;

/// <summary>
/// Consulta la franquicia para una combinación específica de clase + tarifa.
/// Caso de uso clave para informar al pasajero durante la reserva.
/// </summary>
public sealed class GetBaggageAllowanceByCabinAndFareUseCase
{
    private readonly IBaggageAllowanceRepository _repository;

    public GetBaggageAllowanceByCabinAndFareUseCase(IBaggageAllowanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaggageAllowanceAggregate?> ExecuteAsync(
        int               cabinClassId,
        int               fareTypeId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByCabinAndFareAsync(cabinClassId, fareTypeId, cancellationToken);
}
