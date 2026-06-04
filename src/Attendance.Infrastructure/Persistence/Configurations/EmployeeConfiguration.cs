using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="Employee"/> aggregate root.
/// Maps to the <c>Employees</c> table and configures column constraints,
/// the unique username index, and the backing field for the attendance records collection.
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);

        // bcrypt hashes are 60 chars; 256 provides safe headroom for future algorithm changes.
        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // Stored as int — enum numeric values (Employee=1, Admin=2) are stable by design.
        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Enforce unique usernames at the database level (stored lowercase by the domain).
        builder.HasIndex(e => e.Username)
            .IsUnique()
            .HasDatabaseName("UX_Employees_Username");

        // AttendanceRecords is a read-only wrapper around the private _attendanceRecords field.
        // EF Core must write directly to the backing field when loading related data.
        builder.Navigation(e => e.AttendanceRecords)
            .HasField("_attendanceRecords")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
