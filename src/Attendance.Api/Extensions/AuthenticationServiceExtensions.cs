using System.Text;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Domain.Enums;
using Attendance.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Attendance.Api.Extensions;

/// <summary>
/// Extension methods that configure JWT authentication, role-based authorization,
/// and the password hashing and token-generation services.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Registers authentication and authorization services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration containing <c>Authentication</c> section.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>Authentication:SecretKey</c> is missing or shorter than 32 characters.
    /// </exception>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate settings
        services.Configure<AuthenticationSettings>(
            configuration.GetSection(AuthenticationSettings.SectionName));

        var settings = new AuthenticationSettings();
        configuration.GetSection(AuthenticationSettings.SectionName).Bind(settings);

        if (string.IsNullOrWhiteSpace(settings.SecretKey))
            throw new InvalidOperationException(
                $"'{AuthenticationSettings.SectionName}:SecretKey' is required and must not be empty.");

        if (settings.SecretKey.Length < 32)
            throw new InvalidOperationException(
                $"'{AuthenticationSettings.SectionName}:SecretKey' must be at least 32 characters " +
                $"for HMAC-SHA256. Current length: {settings.SecretKey.Length}.");

        // Password hashing — stateless, safe as singletons
        services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();

        // JWT token service — stateless, signing key pre-built in constructor, safe as singleton
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // JWT Bearer authentication
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = false;
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = settings.Issuer,
                    ValidateAudience         = true,
                    ValidAudience            = settings.Audience,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = signingKey,
                    ClockSkew                = TimeSpan.Zero      // No grace period — tokens expire precisely
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Structured log for monitoring authentication failures
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed. Path={Path}",
                            context.HttpContext.Request.Path);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning(
                            "Authorization denied. Path={Path} User={User}",
                            context.HttpContext.Request.Path,
                            context.HttpContext.User.Identity?.Name ?? "unknown");
                        return Task.CompletedTask;
                    }
                };
            });

        // Policy-based authorization
        services.AddAuthorization(options =>
        {
            // Any authenticated employee or admin
            options.AddPolicy("EmployeePolicy", policy =>
                policy.RequireRole(
                    Role.Employee.ToString(),
                    Role.Admin.ToString()));

            // Admin-only operations
            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireRole(Role.Admin.ToString()));
        });

        return services;
    }
}
