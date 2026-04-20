namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AircraftTypeConfiguration : IEntityTypeConfiguration<AircraftTypeEntity>
{
    public void Configure(EntityTypeBuilder<AircraftTypeEntity> builder)
    {
        builder.ToTable("aircraft_type");
        builder.HasKey(e => e.AircraftTypeId);

        builder.Property(e => e.Model).IsRequired().HasMaxLength(50);
        builder.Property(e => e.TotalSeats).IsRequired();
        builder.Property(e => e.CargoCapacityKg).HasColumnType("decimal(10,2)").HasDefaultValue(0m);

        // Índice único compuesto (manufacturer_id, model)
        builder.HasIndex(e => new { e.ManufacturerId, e.Model })
               .IsUnique()
               .HasDatabaseName("uq_aircraft_type");
    }
}
