// Author: Douglass Duncan
// Student Number: ST10473980

using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers;

/// <summary>
/// Provides endpoints for registering and authenticating CoffeeNChill users.
/// </summary>
/// <remarks>
/// Registration details are stored in Azure Table Storage, while successful
/// login attempts produce signed JWT access tokens.
/// <para>Author: Douglass Duncan — ST10473980</para>
/// </remarks>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Provides access to user storage and JWT-generation operations.
    private readonly AuthService _authenticationService;

    // Provides secure password hashing and password verification.
    private readonly IPasswordHasher<UserItem> _passwordHasher;

    // Records registration, login and error information.
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authenticationService">
    /// The service used to create, retrieve and authenticate user accounts.
    /// </param>
    /// <param name="passwordHasher">
    /// The service used to securely hash and verify user passwords.
    /// </param>
    /// <param name="logger">
    /// The logger used to record authentication events and errors.
    /// </param>
    public AuthController(
        AuthService authenticationService,
        IPasswordHasher<UserItem> passwordHasher,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new CoffeeNChill user.
    /// </summary>
    /// <param name="request">
    /// The information required to register the user.
    /// </param>
    /// <returns>
    /// An HTTP response describing whether registration succeeded or failed.
    /// </returns>
    /// <response code="201">
    /// The user was registered successfully.
    /// </response>
    /// <response code="400">
    /// The registration details or requested role are invalid.
    /// </response>
    /// <response code="409">
    /// An account with the supplied details already exists.
    /// </response>
    /// <response code="503">
    /// Azure Table Storage is temporarily unavailable.
    /// </response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        // These are the only roles supported by the CoffeeNChill system.
        string[] allowedRoles =
        {
            "Admin",
            "Staff",
            "Customer"
        };

        // Match the requested role without making the comparison
        // case-sensitive.
        string? normalizedRole = allowedRoles.FirstOrDefault(
            role => role.Equals(
                request.Role.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (normalizedRole is null)
        {
            _logger.LogWarning(
                "Registration rejected because role {Role} is invalid.",
                request.Role);

            return BadRequest(new
            {
                error = "Role must be Admin, Staff or Customer."
            });
        }

        // Email addresses and usernames are normalized to ensure that
        // comparisons remain consistent during registration and login.
        string normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        string normalizedUsername =
            request.Username.Trim().ToLowerInvariant();

        // PartitionKey stores the user's role, while RowKey stores the
        // normalized email address as required by the PoE specification.
        var user = new UserItem
        {
            PartitionKey = normalizedRole,
            RowKey = normalizedEmail,
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            Username = normalizedUsername,
            CreatedDate = DateTimeOffset.UtcNow
        };

        // Only the resulting password hash is stored. The plain-text
        // password is never added to the Azure Table entity.
        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _logger.LogInformation(
            "Attempting to register user {Email} with role {Role}.",
            user.RowKey,
            user.PartitionKey);

        IActionResult result =
            await _authenticationService.CreateUserAsync(user);

        // Return an HTTP 409 response when an account conflict is detected.
        if (result is ConflictObjectResult conflict)
        {
            _logger.LogWarning(
                "Registration conflict for user {Email}.",
                user.RowKey);

            return Conflict(conflict.Value);
        }

        // Preserve any other error status and response body returned by
        // the authentication service.
        if (result is ObjectResult objectResult &&
            objectResult.StatusCode is >= 400)
        {
            return StatusCode(
                objectResult.StatusCode.Value,
                objectResult.Value);
        }

        return result;
    }

    /// <summary>
    /// Authenticates a registered user and issues a signed JWT access token.
    /// </summary>
    /// <param name="request">
    /// The username/email and password supplied by the user.
    /// </param>
    /// <returns>
    /// A JWT access token and its expiration date when authentication
    /// succeeds.
    /// </returns>
    /// <response code="200">
    /// Authentication succeeded and a JWT was generated.
    /// </response>
    /// <response code="401">
    /// The username, email or password is incorrect.
    /// </response>
    /// <response code="500">
    /// An unexpected error occurred while processing the login.
    /// </response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(
        typeof(AuthResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        try
        {
            // Locate the account using either its normalized email address
            // or username.
            UserItem? user =
                await _authenticationService.GetUserAsync(
                    request.UsernameOrEmail);

            // Use the same message for an unknown user and an incorrect
            // password to avoid revealing which accounts are registered.
            if (user is null)
            {
                _logger.LogWarning(
                    "Failed login attempt for {Identifier}.",
                    request.UsernameOrEmail);

                return Unauthorized(new
                {
                    message = "Invalid username/email or password."
                });
            }

            // Compare the supplied password with the securely stored hash.
            PasswordVerificationResult passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    request.Password);

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                _logger.LogWarning(
                    "Failed login attempt for user {Username}.",
                    user.Username);

                return Unauthorized(new
                {
                    message = "Invalid username/email or password."
                });
            }

            // Generate a signed JWT containing the user's identity and role
            // claims after successful password verification.
            AuthResponse authResponse =
                _authenticationService.GenerateToken(user);

            _logger.LogInformation(
                "User {Username} logged in successfully with role {Role}.",
                user.Username,
                user.PartitionKey);

            return Ok(authResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unexpected error occurred during login.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred during login."
                });
        }
    }
}