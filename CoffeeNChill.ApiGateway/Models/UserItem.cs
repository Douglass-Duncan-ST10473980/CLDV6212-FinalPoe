// Author: Douglass Duncan
// Student Number: ST10473980

using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.ApiGateway.Models;

/// <summary>
/// Represents a registered CoffeeNChill user stored in the Azure
/// <c>Users</c> table.
/// </summary>
/// <remarks>
/// The user's role is stored as the <see cref="PartitionKey"/>, while the
/// normalized email address is stored as the <see cref="RowKey"/>.
/// <para>Author: Douglass Duncan — ST10473980</para>
/// </remarks>
public class UserItem : ITableEntity
{
    /// <summary>
    /// Gets or sets the user's authorization role.
    /// </summary>
    /// <remarks>
    /// This value is used as the Azure Table partition key and should be
    /// <c>Admin</c>, <c>Staff</c> or <c>Customer</c>.
    /// </remarks>
    /// <example>Staff</example>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's normalized email address.
    /// </summary>
    /// <remarks>
    /// This value is used as the Azure Table row key and should be trimmed
    /// and converted to lowercase before the entity is stored.
    /// </remarks>
    /// <example>barista@coffeenchill.ac.za</example>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    /// <example>Douglass</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's surname.
    /// </summary>
    /// <example>Duncan</example>
    public string Surname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's normalized and unique username.
    /// </summary>
    /// <example>douglass.duncan</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the securely generated password hash.
    /// </summary>
    /// <remarks>
    /// This property must contain only the result produced by the configured
    /// ASP.NET Core password hasher. A plain-text password must never be
    /// stored in this property.
    /// </remarks>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the account was created.
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the entity timestamp maintained by Azure Table Storage.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the entity tag used by Azure Table Storage for
    /// optimistic-concurrency control.
    /// </summary>
    public ETag ETag { get; set; }
}