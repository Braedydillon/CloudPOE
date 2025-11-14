using Azure;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncredibleComponentsPoe.Models
{
    public class ProductEntity : Azure.Data.Tables.ITableEntity
    {
        public string? PartitionKey { get; set; } = "Products";
        public string? RowKey { get; set; } = string.Empty;
        [Key]public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;

        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }
    }
}
