// done by st10478910
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    public class MenuItemRequestDto
    {
        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Z]{3}-\d{3}$", ErrorMessage = "SKU must be in format XXX-000")]
        public string Sku { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0.01, 10000)]
        public double Price { get; set; }

        public bool IsAvailable { get; set; }
    }
}

