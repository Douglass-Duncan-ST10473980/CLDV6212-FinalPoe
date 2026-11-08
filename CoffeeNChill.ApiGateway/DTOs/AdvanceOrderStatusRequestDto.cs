// done by st10478910
using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.ApiGateway.DTOs
{
    public class AdvanceOrderStatusRequestDto
    {
        [Required]
        public string OrderId { get; set; } = string.Empty;
    }
}
