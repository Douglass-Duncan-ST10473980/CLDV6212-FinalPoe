// written by st10478910
using System.Text;
using System.Text.Json;
using CoffeeNChill.ApiGateway.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller that forwards menu-related requests to the Azure Functions Menu API.
    /// </summary>
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public MenuController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Retrieves all menu items.
        /// </summary>
        /// <returns>A JSON array of menu items.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllMenuItems()
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync("menu");
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Retrieves a single menu item by category and SKU.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="sku">The SKU of the item.</param>
        [HttpGet("{category}/{sku}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMenuItem(string category, string sku)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"menu/item/{category}/{sku}");
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Retrieves all menu items in a specific category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        [HttpGet("/api/menu/category/{category}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMenuItemsByCategory(string category)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"menu/category/{category}");
            var content = await response.Content.ReadAsStringAsync();

            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Creates a new menu item. Requires Staff or Admin role.
        /// </summary>
        /// <param name="request">The menu item data.</param>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateMenuItem([FromBody] MenuItemRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("menu", body);
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Updates an existing menu item. Requires Staff or Admin role.
        /// </summary>
        [HttpPut("{category}/{sku}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMenuItem(string category, string sku, [FromBody] MenuItemRequestDto request)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"menu/{category}/{sku}", body);
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, Content = content, ContentType = "application/json" };
        }

        /// <summary>
        /// Deletes a menu item. Requires Staff or Admin role.
        /// </summary>
        [HttpDelete("{category}/{sku}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteMenuItem(string category, string sku)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.DeleteAsync($"menu/{category}/{sku}");
            return StatusCode((int)response.StatusCode);
        }
    }
}
