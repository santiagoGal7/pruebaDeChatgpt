namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;

/// <summary>
/// Obtiene el check-in de un tiquete.
/// La UNIQUE sobre ticket_id garantiza como máximo un resultado.
/// Útil para verificar si un pasajero ya realizó check-in antes de
/// emitir el boarding pass.
/// </summary>
public sealed class GetCheckInByTicketUseCase
{
    private readonly ICheckInRepository _repository;

    public GetCheckInByTicketUseCase(ICheckInRepository repository)
    {
        _repository = repository;
    }

    public async Task<CheckInAggregate?> ExecuteAsync(
        int               ticketId,
        CancellationToken cancellationToken = default)
        => await _repository.GetByTicketAsync(ticketId, cancellationToken);
}
