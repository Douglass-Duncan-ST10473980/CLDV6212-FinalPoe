using System.Text;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
// Add controller support.
builder.Services.AddControllers();

// Add Swagger/OpenAPI support.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CoffeeNChill API Gateway",
        Version = "v1",
        Description =
            "The secured API Gateway for the CoffeeNChill canteen system."
    });
});

// Register the authentication storage service.
builder.Services.AddSingleton<AuthService>(serviceProvider =>
{
    IConfiguration configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    string connectionString =
        configuration["Storage:ConnectionString"]
        ?? throw new InvalidOperationException(
            "Storage:ConnectionString is missing.");

    ILogger<AuthService> logger =
        serviceProvider.GetRequiredService<
            ILogger<AuthService>>();

    return new AuthService(
        connectionString,
        logger,
        configuration);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = config["JwtSettings:Issuer"],
            ValidAudience = config["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization();
// Register ASP.NET Core's secure password-hashing service.
builder.Services.AddScoped<
    IPasswordHasher<UserItem>,
    PasswordHasher<UserItem>>();


var app = builder.Build();

// Swagger must remain enabled for the VPS demonstration.
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "CoffeeNChill API Gateway v1");

    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();