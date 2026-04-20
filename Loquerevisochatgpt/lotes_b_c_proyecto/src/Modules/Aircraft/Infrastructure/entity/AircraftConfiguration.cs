namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AircraftConfiguration : IEntityTypeConfiguration<AircraftEntity>
{
    public void Configure(EntityTypeBuilder<AircraftEntity> builder)
    {
        builder.ToTable("aircraft");
        builder.HasKey(e => e.AircraftId);

        builder.Property(e => e.RegistrationNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(e => e.RegistrationNumber).IsUnique();

        builder.Property(e => e.ManufactureYear).IsRequired();
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).IsRequired(false);
    }
}
