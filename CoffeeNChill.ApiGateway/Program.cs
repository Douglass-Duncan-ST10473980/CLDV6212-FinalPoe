//done by st10478910
using CoffeeNChill.ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register the base URL for the Functions from configuration
var functionsBaseUrl = builder.Configuration["Functions:BaseUrl"]
    ?? "http://localhost:7071/api";  // fallback for local dev

// Register a typed HttpClient for forwarding requests to Azure Functions
builder.Services.AddHttpClient("FunctionsApi", client =>
{
    client.BaseAddress = new Uri(functionsBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Register global exception handling FIRST so it wraps all downstream middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Swagger — MUST be outside the if() block so it works in Production (Docker)
app.UseSwagger();
app.UseSwaggerUI();

// NOTE: UseHttpsRedirection() has been removed because the Docker
// environment serves the gateway over plain HTTP (port 5000).

app.UseAuthorization();

app.MapControllers();

app.Run();
