using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Google.Protobuf.Compiler;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QueueFunction;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly string _StorageConnectionString;
    private TableClient _tableClient;

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
        _StorageConnectionString = Environment.GetEnvironmentVariable("connection");
        var serviceClient = new TableServiceClient(_StorageConnectionString);
        _tableClient = serviceClient.GetTableClient("PeopleTable");
    }


    [Function(nameof(QueuePeopleSender))]
    public async Task QueuePeopleSender([QueueTrigger("order-table-queue", Connection = "connection")] QueueMessage message)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageId}");


        // Create the table if it doesn't exist
        await _tableClient.CreateIfNotExistsAsync();
        // 1. Manually deserialize the JSON string into our object
        var person = JsonSerializer.Deserialize<PersonEntity>(message.MessageId);
        if (person == null)
        {
            _logger.LogError("Failed to deserialize JSON message.");
            return;
        }
        // 2. CRITICAL STEP: Set the required PartitionKey and RowKey. person. RowKey = Guid. NewGuid().ToString();
        person.RowKey = Guid.NewGuid().ToString();
        person.PartitionKey = "People";
        _logger.LogInformation($"Saving entity with RowKey: {person.RowKey}");
        // 3. Manually add the entity to the table
        _ = await _tableClient.AddEntityAsync(person);
        _logger.LogInformation("Successfully saved person to table.");
    }

    [Function("GetPeople")]
    public async Task<HttpResponseData> GetPeople(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to get all people.");
        try
        {
            // Manually enumerate the AsyncPageable<PersonEntity> to a List<PersonEntity>
            var people = new List<PersonEntity>();
            await foreach (var person in _tableClient.QueryAsync<PersonEntity>())
            {
                people.Add(person);
            }
            // Create an OK (200) response and write the list of people as JSON.
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(people);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query table storage.");
            // Create an error response.
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("An error occurred while retrieving data from the table.");
            return response;
        }
    }
}