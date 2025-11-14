using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

public class BlobStorageFunction
{
    private readonly ILogger<BlobStorageFunction> _logger;

    public BlobStorageFunction(ILogger<BlobStorageFunction> logger)
    {
        _logger = logger;
    }

    [Function("UploadToBlobStorage")]
    public async Task<HttpResponseData> UploadToBlobStorage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uploadblob")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Missing Content-Type header");
                return response;
            }

            var contentType = contentTypeValues.First();
            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;

            if (string.IsNullOrEmpty(boundary))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Missing boundary in Content-Type");
                return response;
            }

            var reader = new MultipartReader(boundary, req.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDisposition = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDisposition && contentDisposition.DispositionType.Equals("form-data") && contentDisposition.FileName.HasValue)
                {
                    var fileName = contentDisposition.FileName.Value;
                    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                    var containerClient = new BlobContainerClient(connectionString, "documents");
                    await containerClient.CreateIfNotExistsAsync();

                    var blobName = $"{Guid.NewGuid()}_{fileName}";
                    var blobClient = containerClient.GetBlobClient(blobName);

                    await using var ms = new MemoryStream();
                    await section.Body.CopyToAsync(ms);
                    ms.Position = 0;

                    await blobClient.UploadAsync(ms, overwrite: true);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"File uploaded successfully as '{blobName}'");
                    return response;
                }

                section = await reader.ReadNextSectionAsync();
            }

            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteStringAsync("No file found in request");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading blob: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("ListBlobs")]
    public async Task<HttpResponseData> ListBlobs(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "listblobs")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var containerClient = new BlobContainerClient(connectionString, "documents");

            if (!await containerClient.ExistsAsync())
            {
                await response.WriteAsJsonAsync(new List<string>());
                return response;
            }

            var blobs = new List<BlobInfo>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobs.Add(new BlobInfo
                {
                    Name = blobItem.Name,
                    Size = blobItem.Properties.ContentLength ?? 0,
                    LastModified = blobItem.Properties.LastModified ?? DateTimeOffset.UtcNow
                });
            }

            await response.WriteAsJsonAsync(blobs);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error listing blobs: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}

public class BlobInfo
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset LastModified { get; set; }
}
