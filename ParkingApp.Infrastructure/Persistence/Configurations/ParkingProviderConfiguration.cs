using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;
using ParkingApp.Domain.Common.Enums;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class ParkingProviderConfiguration : IEntityTypeConfiguration<ParkingProvider>
{
    public void Configure(EntityTypeBuilder<ParkingProvider> builder)
    {
        builder.ToTable("ParkingProviders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderType)
            .IsRequired();

        builder.Property(x => x.ApprovalStatus)
            .IsRequired()
            .HasDefaultValue(ApprovalStatusEnum.Pending);

        builder.Property(x => x.OwnerUserId);

        builder.Property(x => x.OwnerOrganizationId);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.OwnerUser)
            .WithOne(x => x.ParkingProvider)
            .HasForeignKey<ParkingProvider>(x => x.OwnerUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OwnerOrganization)
            .WithOne(x => x.ParkingProvider)
            .HasForeignKey<ParkingProvider>(x => x.OwnerOrganizationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OwnerUserId)
            .IsUnique();

        builder.HasIndex(x => x.OwnerOrganizationId)
            .IsUnique();
    }
}