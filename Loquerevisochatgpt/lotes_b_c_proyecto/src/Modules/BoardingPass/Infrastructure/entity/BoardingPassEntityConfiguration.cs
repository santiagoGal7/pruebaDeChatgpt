namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BoardingPassEntityConfiguration : IEntityTypeConfiguration<BoardingPassEntity>
{
    public void Configure(EntityTypeBuilder<BoardingPassEntity> builder)
    {
        builder.ToTable("boarding_pass");

        builder.HasKey(e => e.Id);

        // PK en SQL es boarding_pass_id [NC-1]
        builder.Property(e => e.Id)
               .HasColumnName("boarding_pass_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.CheckInId)
               .HasColumnName("check_in_id")
               .IsRequired();

        // UNIQUE (check_in_id) — un boarding pass por check-in
        builder.HasIndex(e => e.CheckInId)
               .IsUnique()
               .HasDatabaseName("uq_boarding_pass_check_in");

        builder.Property(e => e.GateId)
               .HasColumnName("gate_id")
               .IsRequired(false);

        builder.Property(e => e.BoardingGroup)
               .HasColumnName("boarding_group")
               .IsRequired(false)
               .HasMaxLength(10);

        // [IR-4] FK → flight_seat (reemplazó seat_confirmed VARCHAR)
        builder.Property(e => e.FlightSeatId)
               .HasColumnName("flight_seat_id")
               .IsRequired();
    }
}
