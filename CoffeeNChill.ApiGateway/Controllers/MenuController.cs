// written by st10478910
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

        /// <summary>
        /// Constructor injects IHttpClientFactory so we can create
        /// a named HttpClient ("FunctionsApi") configured in Program.cs.
        /// </summary>
        public MenuController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

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
    }
}