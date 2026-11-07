using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authenticationService;
    private readonly IPasswordHasher<UserItem> _passwordHasher;
    private readonly ILogger<AuthController> _logger;


    public AuthController(
        AuthService authenticationService,
        IPasswordHasher<UserItem> passwordHasher,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        string[] allowedRoles =
        {
            "Admin",
            "Staff",
            "Customer"
        };

        string? normalizedRole = allowedRoles.FirstOrDefault(role => role.Equals(
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

        string normalizedEmail =
            request.Email.Trim().ToLowerInvariant();

        string normalizedUsername =
            request.Username.Trim().ToLowerInvariant();

        var user = new UserItem
        {
            PartitionKey = normalizedRole,
            RowKey = normalizedEmail,
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            Username = normalizedUsername,
            CreatedDate = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _logger.LogInformation(
            "Attempting to register user {Email} with role {Role}.",
            user.RowKey,
            user.PartitionKey);

        IActionResult result =
            await _authenticationService.CreateUserAsync(user);

        if (result is ConflictObjectResult conflict)
        {
            _logger.LogWarning(
                "Registration conflict for user {Email}.",
                user.RowKey);

            return Conflict(conflict.Value);
        }

        if (result is ObjectResult objectResult &&
            objectResult.StatusCode is >= 400)
        {
            return StatusCode(
                objectResult.StatusCode.Value,
                objectResult.Value);
        }

        return result;
    }

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
            UserItem? user = await _authenticationService.GetUserAsync(request.UsernameOrEmail);

            // Return the same response for an unknown account and an incorrect
            // password. This prevents attackers from discovering valid accounts.
            if (user is null)
            {
                _logger.LogWarning("Failed login attempt for {Identifier}.", request.UsernameOrEmail);

                return Unauthorized(new
                {
                    message = "Invalid username/email or password."
                });
            }

            PasswordVerificationResult passwordResult =
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Failed login attempt for user {Username}.", user.Username);

                return Unauthorized(new
                {
                    message = "Invalid username/email or password."
                });
            }

            AuthResponse authResponse = _authenticationService.GenerateToken(user);

            _logger.LogInformation("User {Username} logged in successfully with role {Role}.", user.Username,
                user.PartitionKey);

            return Ok(authResponse);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected error occurred during login.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An unexpected error occurred during login."
                });
        }
    }
}