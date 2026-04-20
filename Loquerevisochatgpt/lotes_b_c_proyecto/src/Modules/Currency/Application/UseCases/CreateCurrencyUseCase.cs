namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Application.UseCases;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Aggregate;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.Repositories;
using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Domain.ValueObject;
using Sistema_de_gestion_de_tiquetes_Aereos.Shared.Contracts;

public sealed class CreateCurrencyUseCase
{
    private readonly ICurrencyRepository _repository;
    private readonly IUnitOfWork         _unitOfWork;

    public CreateCurrencyUseCase(ICurrencyRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CurrencyAggregate> ExecuteAsync(
        string            isoCode,
        string            name,
        string            symbol,
        CancellationToken cancellationToken = default)
    {
        // CurrencyId(1) es placeholder; EF Core asigna el Id real al insertar.
        var currency = new CurrencyAggregate(new CurrencyId(1), isoCode, name, symbol);

        await _repository.AddAsync(currency, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return currency;
    }
}
