using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must contain between 2 and 50 characters.")]
    [RegularExpression(@"^[\p{L}][\p{L}\p{M}' -]*$", ErrorMessage = "Name contains invalid characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Surname is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Surname must contain between 2 and 50 characters.")]
    [RegularExpression(@"^[\p{L}][\p{L}\p{M}' -]*$", ErrorMessage = "Surname contains invalid characters.")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(120, ErrorMessage = "Email cannot exceed 120 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must contain between 3 and 30 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Username may contain only letters, numbers, periods, underscores and hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must contain between 8 and 100 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        ErrorMessage = "Password must contain an uppercase letter, lowercase letter, number and special character.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression(@"^(?i:Admin|Staff|Customer)$", ErrorMessage = "Role must be Admin, Staff or Customer.")]
    public string Role { get; set; } = string.Empty;
}