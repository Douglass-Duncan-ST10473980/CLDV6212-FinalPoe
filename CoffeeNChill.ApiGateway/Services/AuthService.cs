using System.Net;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.ApiGateway.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.ApiGateway.Services;

public class AuthService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AuthService>? _logger;

    public AuthService(string connectionString, ILogger<AuthService>? logger)
    {
        _logger = logger;

        try
        {
            
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient("Accounts");
            _tableClient.CreateIfNotExists();
            _logger?.LogInformation("MenuItems table initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize MenuItems table.");
            throw;
        }
    }

    public async Task<IActionResult> CreateUserAsync(UserItem user)
    {
        try
        {
            await _tableClient.AddEntityAsync(user);

            return new ObjectResult(new
            {
                message = "User registered successfully."
            })
            {
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (RequestFailedException exception)
            when (exception.Status == 409)
        {
            return new ConflictObjectResult(new
            {
                error = "A user with this email already exists."
            });
        }
    }
    
}