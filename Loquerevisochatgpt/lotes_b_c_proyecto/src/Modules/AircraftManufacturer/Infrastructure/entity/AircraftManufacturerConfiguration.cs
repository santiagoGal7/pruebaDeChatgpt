namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AircraftManufacturerConfiguration : IEntityTypeConfiguration<AircraftManufacturerEntity>
{
    public void Configure(EntityTypeBuilder<AircraftManufacturerEntity> builder)
    {
        builder.ToTable("aircraft_manufacturer");

        // La PK en SQL es manufacturer_id — snake_case lo resuelve automáticamente
        builder.HasKey(e => e.ManufacturerId);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
