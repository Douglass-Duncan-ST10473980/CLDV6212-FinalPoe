// done by st10478910
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    public class UpdateOrderStatusRequestDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
