// written by st10478910
using System.Text;
using System.Text.Json;
using CoffeeNChill.ApiGateway.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller that forwards order-related requests to the
    /// Azure Functions Order API (QueueOrder, GetAllOrders, GetOrderById,
    /// UpdateOrderStatus, AdvanceOrderStatus).
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public OrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// POST /api/orders/queue
        /// Forwards an order to the Functions API, which places it on
        /// the order-processing-queue for asynchronous processing.
        /// </summary>
        [HttpPost("queue")]
        [AllowAnonymous]
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
        /// GET /api/orders
        /// Forwards to the Functions API and returns all orders.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllOrders()
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync("orders");

            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// GET /api/orders/{orderId}
        /// Forwards to the Functions API and returns a single order.
        /// </summary>
        [HttpGet("{orderId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"orders/{orderId}");

            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// PUT /api/orders/status
        /// Forwards to the Functions API to advance an order one step forward
        /// through its lifecycle (Received → Preparing → Ready → Collected).
        /// </summary>
        [HttpPut("status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AdvanceOrderStatus([FromBody] AdvanceOrderStatusRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync("orders/status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new ContentResult { StatusCode = (int)response.StatusCode, Content = responseContent, ContentType = "application/json" };
        }

        /// <summary>
        /// PUT /api/orders/{orderId}/status
        /// Forwards to the Functions API to update an order to a specific status.
        /// </summary>
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, [FromBody] UpdateOrderStatusRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"orders/{orderId}/status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new ContentResult { StatusCode = (int)response.StatusCode, Content = responseContent, ContentType = "application/json" };
        }
    }
}
