// done by st10478910
using System.Net;
using System.Text.Json;

namespace CoffeeNChill.ApiGateway.Middleware
{
    /// <summary>
    /// Global exception handling middleware.
    /// Catches any unhandled exception thrown by a controller or downstream service,
    /// logs it, and returns a consistent JSON error response instead of a raw stack trace.
    ///
    /// This satisfies the "global error-handling middleware" requirement and keeps
    /// the API surface clean and predictable for clients.
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception occurred for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // If headers have already been sent, we can't modify the response.
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started — cannot write error response.");
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var errorBody = new
                {
                    error = "An unexpected error occurred.",
                    path = context.Request.Path.ToString(),
                    method = context.Request.Method,
                    traceId = context.TraceIdentifier
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorBody));
            }
        }
    }
}