// Author: Douglass Duncan
// Student Number: ST10473980

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

/// <summary>
/// Provides user-storage, account-retrieval and JWT-generation operations for
/// the CoffeeNChill API Gateway.
/// </summary>
/// <remarks>
/// User accounts are stored in the Azure <c>Users</c> table. Successful login
/// attempts produce signed JWT access tokens containing name, email and role
/// claims.
/// <para>Author: Douglass Duncan — ST10473980</para>
/// </remarks>
public class AuthService
{
    private const string TableName = "Users";

    // Provides access to user entities stored in Azure Table Storage.
    private readonly TableClient _tableClient;

    // Records account-storage and authentication events.
    private readonly ILogger<AuthService> _logger;

    // Provides access to JWT issuer, audience, signing key and expiry settings.
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="connectionString">
    /// The connection string used to access Azure Table Storage or Azurite.
    /// </param>
    /// <param name="logger">
    /// The logger used to record authentication and storage events.
    /// </param>
    /// <param name="configuration">
    /// The application configuration containing the JWT settings.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the storage connection string is empty.
    /// </exception>
    /// <exception cref="Azure.RequestFailedException">
    /// Thrown when the Users table cannot be initialized.
    /// </exception>
    public AuthService(
        string connectionString,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _logger = logger;
        _configuration = configuration;

        try
        {
            var serviceClient =
                new TableServiceClient(connectionString);

            _tableClient =
                serviceClient.GetTableClient(TableName);

            _tableClient.CreateIfNotExists();

            _logger.LogInformation(
                "{TableName} table initialized successfully.",
                TableName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to initialize the {TableName} table.",
                TableName);

            throw;
        }
    }

    /// <summary>
    /// Creates a new user account in Azure Table Storage.
    /// </summary>
    /// <param name="user">
    /// The user entity containing the account details and password hash.
    /// </param>
    /// <returns>
    /// An HTTP 201 response when the account is created, or an HTTP 409
    /// response when an entity with the same partition and row keys exists.
    /// </returns>
    public async Task<IActionResult> CreateUserAsync(
        UserItem user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await _tableClient.AddEntityAsync(user);

            _logger.LogInformation(
                "Created user {Email} with role {Role}.",
                user.RowKey,
                user.PartitionKey);

            return new ObjectResult(new
            {
                message = "User registered successfully."
            })
            {
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (RequestFailedException exception)
            when (exception.Status ==
                  StatusCodes.Status409Conflict)
        {
            _logger.LogWarning(
                exception,
                "A user with email {Email} and role {Role} already exists.",
                user.RowKey,
                user.PartitionKey);

            return new ConflictObjectResult(new
            {
                error = "A user with this email already exists."
            });
        }
    }

    /// <summary>
    /// Retrieves a registered user by username or email address.
    /// </summary>
    /// <param name="usernameOrEmail">
    /// The username or email address supplied during login.
    /// </param>
    /// <returns>
    /// The matching <see cref="UserItem"/> when found; otherwise,
    /// <see langword="null"/>.
    /// </returns>
    public async Task<UserItem?> GetUserAsync(
        string usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            return null;
        }

        // Registration stores usernames and email addresses in lowercase,
        // so the login identifier is normalized in the same way.
        string normalizedIdentifier =
            usernameOrEmail.Trim().ToLowerInvariant();

        // Email addresses are stored as RowKey values, while usernames are
        // stored in the Username table property.
        string filter = TableClient.CreateQueryFilter(
            $"RowKey eq {normalizedIdentifier} or Username eq {normalizedIdentifier}");

        await foreach (UserItem user in
            _tableClient.QueryAsync<UserItem>(
                filter: filter,
                maxPerPage: 1))
        {
            return user;
        }

        return null;
    }

    /// <summary>
    /// Generates a signed JWT access token for an authenticated user.
    /// </summary>
    /// <param name="user">
    /// The authenticated user whose identity and role will be added to the
    /// token claims.
    /// </param>
    /// <returns>
    /// An <see cref="AuthResponse"/> containing the serialized JWT and its
    /// expiration date.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a required JWT configuration value is missing.
    /// </exception>
    public AuthResponse GenerateToken(UserItem user)
    {
        ArgumentNullException.ThrowIfNull(user);

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

        // These claims allow ASP.NET Core to identify the user and enforce
        // role-based authorization through the Authorize attribute.
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

        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        // Create the JWT using the same issuer, audience and signing key that
        // are configured in Program.cs for token validation.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        string tokenValue =
            new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse
        {
            Token = tokenValue,
            ExpiresAt = expiresAt
        };
    }
}