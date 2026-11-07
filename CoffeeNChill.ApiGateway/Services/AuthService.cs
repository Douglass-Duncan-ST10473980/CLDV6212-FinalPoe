using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.ApiGateway.Services;

public class AuthService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AuthService>? _logger;
    private readonly IConfiguration _configuration;

    public AuthService(string connectionString, ILogger<AuthService>? logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        try
        {
            
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("Accounts");
            _tableClient.CreateIfNotExists();
            _logger?.LogInformation("MenuItems table initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize MenuItems table.");
            throw;
        }
    }

    public async Task<IActionResult> CreateUserAsync(UserItem user)
    {
        try
        {
            await _tableClient.AddEntityAsync(user);

            return new ObjectResult(new
            {
                message = "User registered successfully."
            })
            {
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (RequestFailedException exception)
            when (exception.Status == 409)
        {
            return new ConflictObjectResult(new
            {
                error = "A user with this email already exists."
            });
        }
    }

    public async Task<UserItem?> GetUserAsync(string usernameOrEmail)
    {
        string filter = TableClient.CreateQueryFilter(
            $"Username eq {usernameOrEmail} or Email eq {usernameOrEmail}");

        await foreach (TableEntity user in _tableClient.QueryAsync<TableEntity>(
                           filter: filter,
                           maxPerPage: 1))
        {
            return new UserItem
            {
              PartitionKey = user.PartitionKey,
              RowKey = user.RowKey,
              Timestamp = user.Timestamp,
              Name=user.GetString("Name") ?? string.Empty,
              Surname= user.GetString("Surname") ?? string.Empty,
              Username= user.GetString("UserName") ?? string.Empty,
              PasswordHash= user.GetString("PasswordHash") ?? string.Empty,
              CreatedDate = user.GetDateTime("CreatedDate") ?? DateTime.MinValue,
              
            };
        }

        return null;
    }

    public AuthResponse GenerateToken(UserItem user)
    {
        string issuer =
            _configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Issuer is missing.");

        string audience =
            _configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Audience is missing.");

        string signingKey =
            _configuration["JwtSettings:Key"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Key is missing.");

        int expiryMinutes =
            _configuration.GetValue<int?>(
                "JwtSettings:ExpiryMinutes") ?? 60;

        DateTimeOffset expiresAt =
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.RowKey),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                ClaimTypes.Name,
                user.Username),

            new(
                ClaimTypes.Email,
                user.RowKey),

            new(
                ClaimTypes.Role,
                user.PartitionKey),

            new(
                "fullName",
                $"{user.Name} {user.Surname}".Trim())
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        string tokenValue =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse
        {
            Token = tokenValue,
            ExpiresAt = expiresAt,
        };
    }
    
    
}