using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using IncredibleComponentsPoe.Models;

namespace IncredibleComponentsPoe.Services
{
    public class AzureFileShareServices
    {
        private readonly ShareClient _shareClient;

        public AzureFileShareServices(string connectionString, string fileShareName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(fileShareName))
                throw new ArgumentNullException(nameof(fileShareName));

            _shareClient = new ShareClient(connectionString, fileShareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadFileAsync(string directoryName, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var directory = _shareClient.GetDirectoryClient(directoryName);
            await directory.CreateIfNotExistsAsync();

            var fileClient = directory.GetFileClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(file.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, file.Length), stream);
            }
        }

        public async Task<List<Contract>> ListFilesAsync(string directoryName)
        {
            var results = new List<Contract>();
            var directory = _shareClient.GetDirectoryClient(directoryName);
            await directory.CreateIfNotExistsAsync();

            await foreach (var item in directory.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    var file = directory.GetFileClient(item.Name);
                    var props = await file.GetPropertiesAsync();

                    results.Add(new Contract
                    {
                        Name = item.Name,
                        Size = props.Value.ContentLength,
                        LastModified = props.Value.LastModified
                    });
                }
            }

            return results;
        }

        public async Task DeleteFileAsync(string directoryName, string fileName)
        {
            var directory = _shareClient.GetDirectoryClient(directoryName);
            var fileClient = directory.GetFileClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }

        public async Task<Stream?> DownloadFileAsync(string directoryName, string fileName)
        {
            var directory = _shareClient.GetDirectoryClient(directoryName);
            var fileClient = directory.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var response = await fileClient.DownloadAsync();
                return response.Value.Content;
            }

            return null;
        }
    }
}
