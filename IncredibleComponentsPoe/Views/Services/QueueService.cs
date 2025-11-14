using Azure.Storage.Queues;
using System.Text;

public class QueueService
{
    private readonly QueueClient _queueClient;

    public QueueService(string connectionString, string queueName)
    {
        _queueClient = new QueueClient(connectionString, queueName);
    }

    public async Task SendMessage(string message)
    {
        await _queueClient.CreateIfNotExistsAsync();
        if (await _queueClient.ExistsAsync())
        {
            string encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
            await _queueClient.SendMessageAsync(encodedMessage);
        }
    }
}