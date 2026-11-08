// written by st10478910
using System.Text;
using System.Text.Json;
using CoffeeNChill.ApiGateway.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller that forwards menu-related requests to the
    /// Azure Functions Menu API (Role 1's /api/menu endpoints).
    /// </summary>
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MenuController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ==================== GET ENDPOINTS ====================

        /// <summary>
        /// GET /api/menu
        /// Forwards to the Functions API and returns all menu items.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMenuItems()
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync("menu");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        /// <summary>
        /// GET /api/menu/{category}/{sku}
        /// Forwards to the Functions API and returns a single menu item.
        /// </summary>
        [HttpGet("{category}/{sku}")]
        public async Task<IActionResult> GetMenuItem(string category, string sku)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"menu/{category}/{sku}");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        /// <summary>
        /// GET /api/categories/{category}
        /// Forwards to the Functions API and returns all items in a category.
        /// </summary>
        [HttpGet("/api/categories/{category}")]
        public async Task<IActionResult> GetMenuItemsByCategory(string category)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"categories/{category}");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        // ==================== POST / PUT / DELETE ====================

        /// <summary>
        /// POST /api/menu
        /// Forwards to the Functions API to create a new menu item.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMenuItem([FromBody] MenuItemRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("menu", body);
            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }

        /// <summary>
        /// PUT /api/menu/{category}/{sku}
        /// Forwards to the Functions API to update an existing menu item.
        /// </summary>
        [HttpPut("{category}/{sku}")]
        public async Task<IActionResult> UpdateMenuItem(
            string category, string sku,
            [FromBody] MenuItemRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"menu/{category}/{sku}", body);
            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }

        /// <summary>
        /// DELETE /api/menu/{category}/{sku}
        /// Forwards to the Functions API to delete a menu item.
        /// </summary>
        [HttpDelete("{category}/{sku}")]
        public async Task<IActionResult> DeleteMenuItem(string category, string sku)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.DeleteAsync($"menu/{category}/{sku}");

            return StatusCode((int)response.StatusCode);
        }
    }
}