namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaseFlightEntityConfiguration : IEntityTypeConfiguration<BaseFlightEntity>
{
    public void Configure(EntityTypeBuilder<BaseFlightEntity> builder)
    {
        builder.ToTable("base_flight");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("base_flight_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.FlightCode)
               .HasColumnName("flight_code")
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.AirlineId)
               .HasColumnName("airline_id")
               .IsRequired();

        builder.Property(e => e.RouteId)
               .HasColumnName("route_id")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);

        // UNIQUE (flight_code, airline_id) — espejo de uq_base_flight
        builder.HasIndex(e => new { e.FlightCode, e.AirlineId })
               .IsUnique()
               .HasDatabaseName("uq_base_flight");
    }
}
