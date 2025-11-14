using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

public class BlobService
{
    private readonly string _connectionString;

    public BlobService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"];
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        var containerClient = new BlobContainerClient(_connectionString, containerName);
        await containerClient.CreateIfNotExistsAsync();
        // Removed public access line
        return containerClient;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(Guid.NewGuid() + Path.GetExtension(file.FileName));

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);

        // Generate a SAS URL valid for 1 day
        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(1));

        return sasUri.ToString();
    }

    public async Task DeleteFileAsync(string blobUrl, string containerName)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobName = Path.GetFileName(new Uri(blobUrl).AbsolutePath);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
}
