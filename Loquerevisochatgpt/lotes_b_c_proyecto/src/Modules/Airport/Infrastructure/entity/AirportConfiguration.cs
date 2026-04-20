namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>Configuración EF Core para <see cref="AirportEntity"/>.</summary>
public sealed class AirportConfiguration : IEntityTypeConfiguration<AirportEntity>
{
    public void Configure(EntityTypeBuilder<AirportEntity> builder)
    {
        builder.ToTable("airport");

        builder.HasKey(e => e.AirportId);

        builder.Property(e => e.IataCode)
               .IsRequired()
               .HasMaxLength(3)
               .IsFixedLength();

        builder.HasIndex(e => e.IataCode)
               .IsUnique();

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
