using Azure;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncredibleComponentsPoe.Models
{
    public class OrderEntity : ITableEntity
    {
        [Key] // Use [Key] for EF Core primary key
        public int Id { get; set; }

        public string PartitionKey { get; set; } = "Orders";
        public string? RowKey { get; set; } = Guid.NewGuid().ToString();
        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }

        // Link to customer
        public string CustomerRowKey { get; set; } = string.Empty;

        // Store the username for filtering
        public string CustomerUsername { get; set; } = string.Empty;

        public string ProductRowKeys { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string OrderDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string? Status { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string ProductNames { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }
}
