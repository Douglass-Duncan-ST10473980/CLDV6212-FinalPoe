//done by st10478910
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    public class QueueOrderRequestDto
    {
        [Required]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<string> SelectedItemSKUs { get; set; } = new();

        [Range(0.01, 10000)]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTimeOffset OrderTimestamp { get; set; }
    }
}