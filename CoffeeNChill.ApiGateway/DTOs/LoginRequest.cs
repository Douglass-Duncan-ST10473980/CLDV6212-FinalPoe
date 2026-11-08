// Author: Douglass Duncan
// Student Number: ST10473980

using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs;

/// <summary>
/// Represents the credentials supplied by a user when logging in.
/// </summary>
/// <remarks>
/// Users may identify themselves using either their registered username or
/// email address.
/// <para>Author: Douglass Duncan — ST10473980</para>
/// </remarks>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the registered username or email address.
    /// </summary>
    /// <example>douglass.duncan</example>
    [Required(ErrorMessage = "Username or email is required.")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's plain-text login password.
    /// </summary>
    /// <remarks>
    /// This value is used only to verify the stored password hash. It must
    /// never be stored, logged or included in a JWT.
    /// </remarks>
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}