using CoffeeNChill.ApiGateway.DTOs;
using CoffeeNChill.ApiGateway.Models;
using CoffeeNChill.ApiGateway.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController: ControllerBase
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
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        string[] allowedRoles =
        {
            "Admin",
            "Staff",
            "Customer"
        };

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
}