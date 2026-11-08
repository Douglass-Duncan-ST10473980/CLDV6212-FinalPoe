// done by st10478910
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Controllers
{
    /// <summary>
    /// Gateway controller that forwards document-related requests to the
    /// Azure Functions Document API (upload, list, download).
    ///
    /// Document uploads use multipart/form-data, so the gateway reads the
    /// incoming file stream and forwards it to the Functions using the
    /// same content type.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DocumentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// POST /api/documents/upload
        /// Forwards a multipart/form-data file upload to the Functions API.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file was uploaded." });
            }

            var client = _httpClientFactory.CreateClient("FunctionsApi");

            // Build a new multipart request that mirrors the incoming one
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                file.ContentType ?? "application/octet-stream");

            // The Functions API expects the field name "file"
            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync("documents/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, responseContent);
        }

        /// <summary>
        /// GET /api/documents
        /// Forwards to the Functions API and returns the list of stored documents.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ListDocuments()
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync("documents");

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        /// <summary>
        /// GET /api/documents/download/{fileName}
        /// Forwards to the Functions API and streams the requested file back
        /// to the client (typically a PDF or image).
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadDocument(string fileName)
        {
            var client = _httpClientFactory.CreateClient("FunctionsApi");
            var response = await client.GetAsync($"documents/download/{fileName}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            return File(fileBytes, contentType, fileName);
        }
    }
}
