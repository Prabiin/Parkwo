using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class ParkingFacilityImageConfiguration : IEntityTypeConfiguration<ParkingFacilityImage>
{
    public void Configure(EntityTypeBuilder<ParkingFacilityImage> builder)
    {
        builder.ToTable("ParkingFacilityImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FacilityId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SizeInBytes)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Facility)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FacilityId);
    }
}