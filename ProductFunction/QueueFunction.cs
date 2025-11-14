using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System.Net;
using System.Text.Json;

public class QueueFunctions
{
    private readonly ILogger<QueueFunctions> _logger;

    public QueueFunctions(ILogger<QueueFunctions> logger)
    {
        _logger = logger;
    }

    [Function("SendMessageToQueue")]
    public async Task<HttpResponseData> SendMessageToQueue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sendqueue")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function sending message to queue.");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Empty message body");
                return badResponse;
            }

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var queueName = "message-queue";
            
            var queueClient = new QueueClient(connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();

            // Create a structured message
            var messageData = new QueueMessage
            {
                MessageId = Guid.NewGuid(),
                Content = requestBody,
                Type = "UserMessage",
                Timestamp = DateTime.UtcNow,
                Sender = "AzureFunction"
            };

            var messageJson = JsonSerializer.Serialize(messageData);
            var responseMessage = await queueClient.SendMessageAsync(messageJson);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Message sent to queue successfully. Message ID: {responseMessage.Value.MessageId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending to queue: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }

    [Function("ProcessQueueMessage")]
    public void ProcessQueueMessage(
        [QueueTrigger("message-queue", Connection = "AzureWebJobsStorage")] QueueMessage queueMessage)
    {
        _logger.LogInformation($"C# Queue trigger function processing message: {queueMessage.MessageId}");

        try
        {
            // Process the queue message
            _logger.LogInformation($"Message Type: {queueMessage.Type}");
            _logger.LogInformation($"Message Content: {queueMessage.Content}");
            _logger.LogInformation($"Timestamp: {queueMessage.Timestamp}");
            _logger.LogInformation($"Sender: {queueMessage.Sender}");
            
            // Add your business logic here
            // For example: Save to database, send email, call another service
            
            _logger.LogInformation($"Message processed successfully at: {DateTime.Now}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing queue message: {ex.Message}");
        }
    }

    [Function("GetQueueMessages")]
    public async Task<HttpResponseData> GetQueueMessages(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "getqueuemessages")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function getting queue messages.");

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var queueName = "message-queue";
            
            var queueClient = new QueueClient(connectionString, queueName);
            
            if (!await queueClient.ExistsAsync())
            {
                await req.CreateResponse(HttpStatusCode.OK).WriteAsJsonAsync(new List<QueueMessage>());
                return req.CreateResponse(HttpStatusCode.OK);
            }

            var messages = new List<QueueMessage>();
            var receivedMessages = await queueClient.ReceiveMessagesAsync(10);

            foreach (var message in receivedMessages.Value)
            {
                try
                {
                    var queueMessage = JsonSerializer.Deserialize<QueueMessage>(message.MessageText);
                    if (queueMessage != null)
                    {
                        messages.Add(queueMessage);
                    }
                }
                catch
                {
                    // Handle non-JSON messages
                    messages.Add(new QueueMessage
                    {
                        MessageId = Guid.NewGuid(),
                        Content = message.MessageText,
                        Type = "RawMessage",
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(messages);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting queue messages: {ex.Message}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

public class QueueMessage
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Sender { get; set; } = string.Empty;
}