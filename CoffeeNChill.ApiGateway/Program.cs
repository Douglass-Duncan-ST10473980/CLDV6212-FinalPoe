// done by st10478910
using CoffeeNChill.ApiGateway.Middleware;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var functionsBaseUrl = builder.Configuration["Functions:BaseUrl"] ?? "http://localhost:7071/api";

// Keep the HttpClient configuration
builder.Services.AddHttpClient("FunctionsApi", client =>
{
    client.BaseAddress = new Uri(functionsBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddEndpointsApiExplorer();

// Enable XML documentation for Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoffeeNChill API Gateway", Version = "v1" });

    // Tell Swagger to read the XML comments (summary tags) from the code
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Keep the global exception middleware first
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

// NOTE: Douglas will add app.UseAuthentication() and app.UseAuthorization() here when he merges his JWT code.

app.MapControllers();

app.Run();
