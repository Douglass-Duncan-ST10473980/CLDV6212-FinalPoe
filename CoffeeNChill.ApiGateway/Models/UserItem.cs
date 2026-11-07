using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models;

public class UserItem :ITableEntity
{
    //Role
    public string PartitionKey { get; set; }
    
    //Email
    public string RowKey { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}