using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username or Email is required")]
    public string UsernameOrEmail  { get; set; }
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
}