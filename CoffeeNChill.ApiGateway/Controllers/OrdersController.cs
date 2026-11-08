// written by st10478910
using System.Text;
using System.Text.Json;
using CoffeeNChill.ApiGateway.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller that forwards order-related requests to the Azure Functions Order API.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Places a new order on the queue for processing.
        /// </summary>
        [HttpPost("queue")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> QueueOrder([FromBody] QueueOrderRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("orders/queue", body);
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Retrieves all orders. Requires Staff or Admin role.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllOrders()
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync("orders");
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Retrieves a specific order by ID. Requires Staff or Admin role.
        /// </summary>
        [HttpGet("{orderId}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"orders/{orderId}");
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Advances an order one step forward in the lifecycle. Requires Staff or Admin role.
        /// </summary>
        [HttpPut("status")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AdvanceOrderStatus([FromBody] AdvanceOrderStatusRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync("orders/status", body);
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Updates an order to a specific status. Requires Staff or Admin role.
        /// </summary>
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, [FromBody] UpdateOrderStatusRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"orders/{orderId}/status", body);
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }
    }
}
