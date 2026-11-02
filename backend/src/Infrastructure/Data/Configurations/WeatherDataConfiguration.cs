using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class WeatherDataConfiguration : IEntityTypeConfiguration<WeatherData>
{
    public void Configure(EntityTypeBuilder<WeatherData> builder)
    {
        builder.ToTable("weather_data");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(w => w.Location)
            .HasColumnName("location")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Condition)
            .HasColumnName("condition")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.TemperatureCelsius)
            .HasColumnName("temperature_celsius")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(w => w.CloudCoverPercentage)
            .HasColumnName("cloud_cover_percentage")
            .HasPrecision(5, 2);

        // Unique constraint: one weather record per location per date
        builder.HasIndex(w => new { w.Date, w.Location })
            .IsUnique()
            .HasDatabaseName("ix_weather_data_date_location");
    }
}