using Azure;
using Azure.Data.Tables;
using System;

namespace IncredibleComponentsPoe.Models
{
    public class CartItem : ITableEntity
    {
        public string PartitionKey { get; set; } = "Cart";
        public string? RowKey { get; set; } = Guid.NewGuid().ToString();
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

        public string CustomerUsername { get; set; } = string.Empty; // The logged-in customer's username
        public string ProductRowKey { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Total => Price * Quantity;
    }
}
