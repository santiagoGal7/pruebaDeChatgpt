namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CheckInEntityConfiguration : IEntityTypeConfiguration<CheckInEntity>
{
    public void Configure(EntityTypeBuilder<CheckInEntity> builder)
    {
        builder.ToTable("check_in");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("check_in_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.TicketId)
               .HasColumnName("ticket_id")
               .IsRequired();

        // UNIQUE (ticket_id) — un tiquete = un check-in
        builder.HasIndex(e => e.TicketId)
               .IsUnique()
               .HasDatabaseName("uq_check_in_ticket");

        builder.Property(e => e.CheckInTime)
               .HasColumnName("check_in_time")
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CheckInStatusId)
               .HasColumnName("check_in_status_id")
               .IsRequired();

        builder.Property(e => e.CounterNumber)
               .HasColumnName("counter_number")
               .IsRequired(false)
               .HasMaxLength(20);
    }
}
