// Author: Douglass Duncan
// Student Number: ST10473980

namespace CoffeeNChill.ApiGateway.DTOs;

/// <summary>
/// Represents the response returned after a user successfully logs in.
/// </summary>
/// <remarks>
/// The access token is used as a Bearer token when calling protected Gateway
/// endpoints.
/// <para>Author: Douglass Duncan — ST10473980</para>
/// </remarks>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets the signed JWT access token.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time at which the access token expires.
    /// </summary>
    /// <example>2026-11-07T18:27:23Z</example>
    public DateTimeOffset ExpiresAt { get; set; }
}