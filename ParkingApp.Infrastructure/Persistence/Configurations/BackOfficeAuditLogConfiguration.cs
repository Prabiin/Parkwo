using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain.BackOffice.Audit;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class BackOfficeAuditLogConfiguration : IEntityTypeConfiguration<BackOfficeAuditLog>
{
    public void Configure(EntityTypeBuilder<BackOfficeAuditLog> builder)
    {
        builder.ToTable("BackOfficeAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.ActorUserId);

        builder.Property(x => x.ActorUserName)
            .HasMaxLength(200);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EntityId);

        builder.Property(x => x.Details)
            .HasMaxLength(2000);

        // The console reads the trail newest-first, usually filtered by who did
        // it or what they touched.
        builder.HasIndex(x => x.OccurredAtUtc)
            .IsDescending();

        builder.HasIndex(x => x.ActorUserId);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
