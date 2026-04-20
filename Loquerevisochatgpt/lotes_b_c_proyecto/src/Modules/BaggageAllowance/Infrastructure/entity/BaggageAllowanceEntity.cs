namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;

public sealed class BaggageAllowanceEntity
{
    public int     Id            { get; set; }
    public int     CabinClassId  { get; set; }
    public int     FareTypeId    { get; set; }
    public int     CarryOnPieces { get; set; }
    public decimal CarryOnKg     { get; set; }
    public int     CheckedPieces { get; set; }
    public decimal CheckedKg     { get; set; }
}
