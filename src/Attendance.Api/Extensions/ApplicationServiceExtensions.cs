using Attendance.Application.DTOs;
using FluentValidation;

namespace Attendance.Api.Extensions;

/// <summary>
/// Extension methods that register pure application-layer services:
/// FluentValidation validators, MVC controllers, and framework utilities.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers application-layer services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration (reserved for future use).</param>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MVC + OpenAPI discovery
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();

        // Auto-register every AbstractValidator<T> found in the Application assembly.
        // The assembly is located by the type of any known DTO from that project.
        services.AddValidatorsFromAssemblyContaining<LoginRequest>(ServiceLifetime.Scoped);

        return services;
    }
}
