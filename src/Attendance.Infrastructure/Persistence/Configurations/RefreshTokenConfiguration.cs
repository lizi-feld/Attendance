using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="RefreshToken"/> entity.
/// Maps to the <c>RefreshTokens</c> table with a unique token index and a cascade-delete FK to Employees.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .UseIdentityColumn();

        // 64 random bytes → 88 base64 chars. 512 provides headroom for future formats.
        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(r => r.EmployeeId)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Nullable — null means the token has not been revoked.
        builder.Property(r => r.RevokedAt)
            .HasColumnType("datetime2");

        // IsRevoked is a computed read-only property — explicitly ignored for clarity.
        // IsActiveAt is a method, not a property — EF Core never maps methods.
        builder.Ignore(r => r.IsRevoked);

        // Unique index for O(1) token lookup during auth and revocation.
        builder.HasIndex(r => r.Token)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_Token");

        // Supports RevokeAllForEmployeeAsync and employee-scoped queries.
        builder.HasIndex(r => r.EmployeeId)
            .HasDatabaseName("IX_RefreshTokens_EmployeeId");

        // Unidirectional relationship: RefreshToken → Employee.
        // Employee does not expose a RefreshTokens collection.
        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
