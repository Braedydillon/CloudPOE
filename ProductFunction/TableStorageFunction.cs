using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Net;
using System.Text.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TableStorageFunction
{
    private readonly ILogger<TableStorageFunction> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public TableStorageFunction(ILogger<TableStorageFunction> logger)
    {
        _logger = logger;
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        _tableServiceClient = new TableServiceClient(connectionString);
    }

    [Function("StoreCustomerInTable")]
    public async Task<HttpResponseData> StoreCustomerInTable(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "storecustomer")] HttpRequestData req)
    {
        _logger.LogInformation("Storing customer in Table Storage.");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customerData = JsonSerializer.Deserialize<CustomerData>(requestBody);

            if (customerData == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer data");
                return badResponse;
            }

            // Create TableClient and ensure table exists
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.CreateIfNotExistsAsync();

            // Create customer entity
            var customerEntity = new CustomerEntity
            {
                PartitionKey = "Customer",
                RowKey = Guid.NewGuid().ToString(),
                Name = customerData.Name,
                Email = customerData.Email,
                Phone = customerData.Phone,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Add to table
            await tableClient.AddEntityAsync(customerEntity);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Customer '{customerData.Name}' stored successfully with ID: {customerEntity.RowKey}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error storing customer: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("GetCustomersFromTable")]
    public async Task<HttpResponseData> GetCustomersFromTable(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getcustomers")] HttpRequestData req)
    {
        _logger.LogInformation("Retrieving customers from Table Storage.");

        try
        {
            var tableClient = _tableServiceClient.GetTableClient("Customers");
            await tableClient.CreateIfNotExistsAsync(); // Ensure table exists

            var customers = new List<CustomerEntity>();
            await foreach (var customer in tableClient.QueryAsync<CustomerEntity>())
            {
                customers.Add(customer);
            }

            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteAsJsonAsync(customers);
            return successResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving customers: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error retrieving data from table");
            return errorResponse;
        }
    }
}

// DTO for incoming customer data
public class CustomerData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

// Table entity representing a customer
public class CustomerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Customer";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
