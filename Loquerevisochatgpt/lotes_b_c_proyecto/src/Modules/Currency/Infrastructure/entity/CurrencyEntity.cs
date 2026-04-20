namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;

public sealed class CurrencyEntity
{
    public int    Id      { get; set; }
    public string IsoCode { get; set; } = null!;
    public string Name    { get; set; } = null!;
    public string Symbol  { get; set; } = null!;
}
