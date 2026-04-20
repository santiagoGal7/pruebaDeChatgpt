namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;

public sealed class BaggageTypeEntity
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = null!;
    public decimal MaxWeightKg { get; set; }
    public decimal ExtraFee    { get; set; }
}
