using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VehicleType)
            .IsRequired();

        builder.Property(x => x.VehicleCategory)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.VehicleNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Color)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.VehicleNumber);
    }
}