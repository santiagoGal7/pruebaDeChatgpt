namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;

public sealed class CustomerEntity
{
    public int       Id        { get; set; }
    public int       PersonId  { get; set; }
    public string?   Phone     { get; set; }
    public string?   Email     { get; set; }
    public DateTime  CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
