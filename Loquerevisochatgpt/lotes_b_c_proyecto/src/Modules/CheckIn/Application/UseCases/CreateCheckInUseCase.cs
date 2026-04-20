namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCheckInUseCase
{
    private readonly ICheckInRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public CreateCheckInUseCase(ICheckInRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckInAggregate> ExecuteAsync(
        int               ticketId,
        int               checkInStatusId,
        string?           counterNumber,
        CancellationToken cancellationToken = default)
    {
        // CheckInId(1) es placeholder; EF Core asigna el Id real al insertar.
        var checkIn = new CheckInAggregate(
            new CheckInId(1),
            ticketId,
            DateTime.UtcNow,
            checkInStatusId,
            counterNumber);

        await _repository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return checkIn;
    }
}
