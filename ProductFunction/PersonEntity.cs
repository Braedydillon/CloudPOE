using Azure;
using Azure.Data.Tables;

namespace QueueFunction // Changed to match Function1 namespace
{
    public class PersonEntity : ITableEntity // Changed to public
    {
        // These properties will be populated from your JSON
        public string? Name { get; set; }
        public string? Email { get; set; }

        // --- Required Table Storage Properties
        public string PartitionKey { get; set; } = "People"; // Changed default to "People"
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}