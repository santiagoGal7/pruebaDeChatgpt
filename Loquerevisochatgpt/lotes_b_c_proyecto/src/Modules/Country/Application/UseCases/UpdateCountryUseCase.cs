namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCountryUseCase
{
    private readonly ICountryRepository _repository;
    private readonly IUnitOfWork        _unitOfWork;

    public UpdateCountryUseCase(ICountryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            newName,
        CancellationToken cancellationToken = default)
    {
        var country = await _repository.GetByIdAsync(new CountryId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Country with id {id} was not found.");

        country.UpdateName(newName);
        await _repository.UpdateAsync(country, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
