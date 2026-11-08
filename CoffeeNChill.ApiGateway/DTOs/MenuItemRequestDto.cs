// done by st10478910
namespace CoffeeNChill.ApiGateway.DTOs
{
    /// <summary>
    /// Request body for creating or updating a menu item via the Gateway.
    /// This mirrors the shape expected by the Functions Menu API.
    /// </summary>
    public class MenuItemRequestDto
    {
        public string Category { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}