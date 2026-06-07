using Microsoft.OpenApi.Models;

namespace Attendance.Api.Extensions;

/// <summary>
/// Extension methods that configure Swagger / OpenAPI documentation,
/// JWT security definitions, and interactive authorization support in Swagger UI.
/// </summary>
public static class SwaggerServiceExtensions
{
    private const string SecuritySchemeName = "Bearer";

    /// <summary>
    /// Registers SwaggerGen with XML documentation, JWT security definitions,
    /// and annotation support for <c>[SwaggerOperation]</c> and <c>[SwaggerResponse]</c>.
    /// </summary>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Time Attendance API",
                Version     = "v1",
                Description =
                    "RESTful API for the Time Attendance System.\n\n" +
                    "All attendance timestamps originate from an external time provider " +
                    "(Europe/Zurich timezone). Local server time is never used for attendance records.\n\n" +
                    "**Authentication:** use `POST /api/auth/login` to obtain a Bearer token, " +
                    "then click **Authorize** and enter `Bearer {token}`.",
                Contact = new OpenApiContact
                {
                    Name  = "System Administrator",
                    Email = "admin@attendance.example.com"
                }
            });

            // Enable [SwaggerOperation] and [SwaggerResponse] attributes on controllers.
            options.EnableAnnotations();

            // Include XML doc comments from the API project (controller summaries, param docs).
            var apiXmlPath = Path.Combine(
                AppContext.BaseDirectory,
                "Attendance.Api.xml");
            if (File.Exists(apiXmlPath))
                options.IncludeXmlComments(apiXmlPath, includeControllerXmlComments: true);

            // Include XML doc comments from the Application project (DTO property summaries).
            var appXmlPath = Path.Combine(
                AppContext.BaseDirectory,
                "Attendance.Application.xml");
            if (File.Exists(appXmlPath))
                options.IncludeXmlComments(appXmlPath);

            // JWT Bearer security definition
            options.AddSecurityDefinition(SecuritySchemeName, new OpenApiSecurityScheme
            {
                Description  = "JWT Bearer token. Format: **Bearer {token}**",
                Name         = "Authorization",
                In           = ParameterLocation.Header,
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT"
            });

            // Apply JWT requirement globally — every endpoint shows the lock icon.
            // Anonymous endpoints ([AllowAnonymous]) still work; the header is simply ignored.
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = SecuritySchemeName
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the Swagger UI middleware with persistent authorization,
    /// so the Bearer token is not lost between page refreshes during development.
    /// Call this after <c>app.UseSwagger()</c>.
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Time Attendance API v1");
            ui.RoutePrefix = "swagger";
            ui.DocumentTitle = "Time Attendance API";

            // Retain the entered Bearer token across browser refreshes.
            ui.ConfigObject.AdditionalItems["persistAuthorization"] = true;

            // Collapse the model schema panel by default to reduce visual noise.
            ui.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}
