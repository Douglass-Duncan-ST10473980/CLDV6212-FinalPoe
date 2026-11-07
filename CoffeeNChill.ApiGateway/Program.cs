// CoffeeNChill API Gateway application configuration
//
// Contributors:
// - Douglass Duncan — ST10473980
//   Authentication service registration, password hashing and JWT validation.
//
// - [GROUP MEMBER FULL NAME] — [STUDENT NUMBER]
//   [Describe contribution, for example: API Gateway and Swagger configuration.]
//
// - [GROUP MEMBER FULL NAME] — [STUDENT NUMBER]
//   [Describe contribution, for example: deployment and production configuration.]
//
// Add or update contributor details when branches are merged. Do not remove
// another member's contribution information.

using System.Text;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Provides access to appsettings files, environment variables and user
// secrets throughout the application.
IConfiguration configuration = builder.Configuration;


// Register support for attribute-routed ASP.NET Core API controllers.
builder.Services.AddControllers();


// SWAGGER / OPENAPI CONFIGURATION

// Registers services required to discover and describe API endpoints.
builder.Services.AddEndpointsApiExplorer();

// Generates the OpenAPI document displayed through Swagger UI.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "CoffeeNChill API Gateway",
        Version = "v1",
        Description =
            "The secured API Gateway for the CoffeeNChill canteen system."
    });

    // TODO: Add the Swagger JWT Bearer security definition so that the
    // Authorize button can be used to test protected endpoints.
});

// -------------------------------------------------------------------------
// AUTHENTICATION STORAGE SERVICE
// Contributor: Douglass Duncan — ST10473980
// -------------------------------------------------------------------------

// Register one AuthService instance for the lifetime of the application.
// The service creates and accesses the Users Azure Table and generates JWTs.
builder.Services.AddSingleton<AuthService>(
    serviceProvider =>
    {
        IConfiguration serviceConfiguration =
            serviceProvider.GetRequiredService<IConfiguration>();

        string connectionString =
            serviceConfiguration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Storage:ConnectionString is missing.");

        ILogger<AuthService> logger =
            serviceProvider.GetRequiredService<
                ILogger<AuthService>>();

        return new AuthService(
            connectionString,
            logger,
            serviceConfiguration);
    });

// -------------------------------------------------------------------------
// JWT BEARER AUTHENTICATION
// Contributor: Douglass Duncan — ST10473980
// -------------------------------------------------------------------------

string jwtIssuer =
    configuration["JwtSettings:Issuer"]
    ?? throw new InvalidOperationException(
        "JwtSettings:Issuer is missing.");

string jwtAudience =
    configuration["JwtSettings:Audience"]
    ?? throw new InvalidOperationException(
        "JwtSettings:Audience is missing.");

string jwtKey =
    configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException(
        "JwtSettings:Key is missing.");

// Configure the Gateway to read and validate JWTs supplied through the
// Authorization: Bearer <token> request header.
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Confirm that the token was issued by this Gateway.
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                // Confirm that the token was created for the expected client.
                ValidateAudience = true,
                ValidAudience = jwtAudience,

                // Reject tokens after their expiration date.
                ValidateLifetime = true,

                // Verify that the token signature was produced using the
                // configured CoffeeNChill signing key.
                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                // Allow only a small difference between server clocks.
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

// Register role-based and policy-based authorization support.
builder.Services.AddAuthorization();

// Register ASP.NET Core's secure password-hashing implementation.
// A new lightweight hasher can be provided wherever it is requested.
builder.Services.AddScoped<
    IPasswordHasher<UserItem>,
    PasswordHasher<UserItem>>();


// ADDITIONAL GATEWAY SERVICES


var app = builder.Build();

// -------------------------------------------------------------------------
// HTTP REQUEST PIPELINE
// Shared file — update contributor details above when modifying this section.
// -------------------------------------------------------------------------

// Swagger remains enabled outside development so that the live VPS
// deployment can be demonstrated as required by the Part 3 rubric.
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "CoffeeNChill API Gateway v1");

    options.RoutePrefix = "swagger";

    // Retain the Bearer token entered through Swagger's Authorize dialog
    // when the page is refreshed.
    options.EnablePersistAuthorization();
});

// Redirect HTTP requests to HTTPS when an HTTPS endpoint is available.
app.UseHttpsRedirection();

// Authentication must execute before authorization. Authentication validates
// the JWT and creates HttpContext.User. Authorization then checks attributes
// such as [Authorize] and [Authorize(Roles = "Admin,Staff")].
app.UseAuthentication();
app.UseAuthorization();

// Map attribute-routed controllers such as /api/auth/register and
// /api/auth/login.
app.MapControllers();

// Start the CoffeeNChill API Gateway.
app.Run();