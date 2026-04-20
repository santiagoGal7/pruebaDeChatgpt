namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class UpdateCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public UpdateCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        int               id,
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        var currency = await _repository.GetByIdAsync(new CurrencyId(id), cancellationToken)
            ?? throw new KeyNotFoundException($"Currency with id {id} was not found.");

        currency.Update(isoCode, name, symbol);
        await _repository.UpdateAsync(currency, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}
