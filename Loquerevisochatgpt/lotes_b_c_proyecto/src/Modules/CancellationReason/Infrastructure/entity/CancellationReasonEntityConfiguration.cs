namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CancellationReason.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CancellationReasonEntityConfiguration : IEntityTypeConfiguration<CancellationReasonEntity>
{
    public void Configure(EntityTypeBuilder<CancellationReasonEntity> builder)
    {
        builder.ToTable("cancellation_reason");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("cancellation_reason_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_cancellation_reason_name");
    }
}
