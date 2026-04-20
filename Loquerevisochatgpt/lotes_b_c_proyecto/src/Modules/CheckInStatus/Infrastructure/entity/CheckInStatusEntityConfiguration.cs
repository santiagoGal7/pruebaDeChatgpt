namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckInStatus.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CheckInStatusEntityConfiguration : IEntityTypeConfiguration<CheckInStatusEntity>
{
    public void Configure(EntityTypeBuilder<CheckInStatusEntity> builder)
    {
        builder.ToTable("check_in_status");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("check_in_status_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_check_in_status_name");
    }
}
