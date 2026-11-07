namespace CoffeeNChill.ApiGateway.DTOs;

public class LoginResponse
{
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}