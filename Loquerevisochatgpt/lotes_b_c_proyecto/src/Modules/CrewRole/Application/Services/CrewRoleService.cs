namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Services;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Application.Interfaces;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CrewRoleService : ICrewRoleService
{
    private readonly ICrewRoleRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public CrewRoleService(ICrewRoleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CrewRoleDto> CreateAsync(CreateCrewRoleRequest request, CancellationToken ct = default)
    {
        var entity = CrewRoleAggregate.Create(request.Name);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.CommitAsync(ct);
        return Map(entity);
    }

    public async Task<CrewRoleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(CrewRoleId.New(id), ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<CrewRoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _repository.GetAllAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<CrewRoleDto> UpdateAsync(int id, UpdateCrewRoleRequest request, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(CrewRoleId.New(id), ct)
            ?? throw new KeyNotFoundException($"CrewRole with id {id} not found.");
        entity.Update(request.Name);
        _repository.Update(entity);
        await _unitOfWork.CommitAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(CrewRoleId.New(id), ct)
            ?? throw new KeyNotFoundException($"CrewRole with id {id} not found.");
        _repository.Delete(entity);
        await _unitOfWork.CommitAsync(ct);
    }

    private static CrewRoleDto Map(CrewRoleAggregate a) => new(a.Id.Value, a.Name);
}
