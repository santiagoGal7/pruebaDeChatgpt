namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CrewRoleConfiguration : IEntityTypeConfiguration<CrewRoleEntity>
{
    public void Configure(EntityTypeBuilder<CrewRoleEntity> builder)
    {
        builder.ToTable("crew_role");
        builder.HasKey(e => e.CrewRoleId);

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(50);
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
