using Attendance.Api.Extensions;
using Attendance.Api.Middleware;

// ── Builder ────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddSwaggerServices();

// ── App ────────────────────────────────────────────────────────────────────
var app = builder.Build();

await app.MigrateAndSeedAsync();

// 1. Global exception handler — must be first to catch errors from all middleware below.
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 2. Swagger — development only.
if (app.Environment.IsDevelopment())
    app.UseSwaggerWithUi();

// 3. HTTPS redirection.
app.UseHttpsRedirection();

// 4. Authentication — must precede Authorization.
app.UseAuthentication();

// 5. Authorization.
app.UseAuthorization();

// 6. Health checks.
app.MapHealthChecks("/health");

// 7. Controllers.
app.MapControllers();

app.Run();
