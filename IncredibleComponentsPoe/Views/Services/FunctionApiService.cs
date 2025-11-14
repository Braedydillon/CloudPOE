using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MvcApp.Services
{
    public class FunctionApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FunctionApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["AzureFunctions:BaseUrl"];
        }

        // Store customer in Table
        public async Task<string> StoreCustomerAsync(object customer)
        {
            var json = JsonSerializer.Serialize(customer);
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/StoreToTable",
                new StringContent(json, Encoding.UTF8, "application/json"));

            return await response.Content.ReadAsStringAsync();
        }

        // Upload to Blob
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            var response = await _httpClient.PostAsync($"{_baseUrl}/UploadToBlob", content);
            return await response.Content.ReadAsStringAsync();
        }

        // Send message to Queue
        public async Task<string> SendToQueueAsync(string message)
        {
            var content = new StringContent(message, Encoding.UTF8, "text/plain");
            var response = await _httpClient.PostAsync($"{_baseUrl}/SendToQueue", content);
            return await response.Content.ReadAsStringAsync();
        }

        // Upload file to File Share
        public async Task<string> UploadToFileShareAsync(IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            content.Add(new StreamContent(stream), "file", file.FileName);

            var response = await _httpClient.PostAsync($"{_baseUrl}/SaveToFileShare", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
