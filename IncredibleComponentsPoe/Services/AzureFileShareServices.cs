using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using IncredibleComponentsPoe.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IncredibleComponentsPoe.Services
{
    public class AzureFileShareService
    {
        private readonly ShareClient _shareClient;

        // DI-friendly constructor
        public AzureFileShareService(IConfiguration configuration)
        {
            // Get connection string and share name from appsettings.json
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=st10439231storageaccount;AccountKey=FoKyxXcGSYlBySKwM5/pslFaNJ91T69s+iaFHz2ypfvXNcepDdhKl7U30dq8ha/IfWSG1oYIamvC+AStOuRyHQ==;EndpointSuffix=core.windows.net";
            var fileShareName = configuration["AzureFileStorageSettings:ShareName"];

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

            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(file.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, file.Length), stream);
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
