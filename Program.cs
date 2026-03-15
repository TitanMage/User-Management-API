using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Register in-memory user repository
builder.Services.AddSingleton<UserManagementAPI.Repositories.IUserRepository, UserManagementAPI.Repositories.InMemoryUserRepository>();
// Add logging
builder.Services.AddLogging();
// Register token service for JWT generation and validation
builder.Services.AddScoped<UserManagementAPI.Services.ITokenService, UserManagementAPI.Services.TokenService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Use global exception handling middleware (outermost - catches all exceptions)
app.UseMiddleware<UserManagementAPI.Middleware.GlobalExceptionHandlingMiddleware>();

// Use token validation middleware (validates authorization)
app.UseMiddleware<UserManagementAPI.Middleware.TokenValidationMiddleware>();

// Use HTTP logging middleware (logs all requests/responses)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<UserManagementAPI.Middleware.HttpLoggingMiddleware>();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
