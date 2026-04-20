namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class AircraftService : IAircraftService
{
    private readonly IAircraftRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public AircraftService(IAircraftRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository; _unitOfWork = unitOfWork;
    }

    public async Task<AircraftDto> CreateAsync(CreateAircraftRequest request, CancellationToken cancellationToken = default)
    {
        var aircraft = AircraftAggregate.Create(request.AirlineId, request.AircraftTypeId, request.RegistrationNumber, request.ManufactureYear, request.IsActive);
        await _repository.AddAsync(aircraft, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(aircraft);
    }

    public async Task<AircraftDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var a = await _repository.GetByIdAsync(AircraftId.New(id), cancellationToken);
        return a is null ? null : Map(a);
    }

    public async Task<IReadOnlyList<AircraftDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<AircraftDto> UpdateAsync(int id, UpdateAircraftRequest request, CancellationToken cancellationToken = default)
    {
        var aircraft = await _repository.GetByIdAsync(AircraftId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Aircraft {id} not found.");
        aircraft.Update(request.AirlineId, request.AircraftTypeId, request.RegistrationNumber, request.ManufactureYear, request.IsActive);
        _repository.Update(aircraft);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Map(aircraft);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var aircraft = await _repository.GetByIdAsync(AircraftId.New(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Aircraft {id} not found.");
        _repository.Delete(aircraft);
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private static AircraftDto Map(AircraftAggregate a) =>
        new(a.Id.Value, a.AirlineId, a.AircraftTypeId, a.RegistrationNumber, a.ManufactureYear, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
