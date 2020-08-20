using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Uploader
{
    public class AzureContentDestination : IContentDestination
    {
        private readonly BlobContainerClient _container;
        private readonly IMimeTypeMap _mimeTypes;
        private readonly ILogger _logger;

        public AzureContentDestination(
            AzureConfiguration config,
            IMimeTypeMap mimeTypeMap,
            ILogger<AzureContentDestination> logger)
        {
            _mimeTypes = mimeTypeMap;
            _logger = logger;

            _container = new BlobContainerClient(config.ConnectionString, "$web");
        }

        public async Task DeleteFileAsync(string path)
        {
            _logger.LogInformation("Deleting file: " + path);

            await _container.GetBlobClient(path).DeleteAsync();
        }

        public async Task<CloudFileInfo?> GetFileAsync(string path)
        {
            var blob = _container.GetBlobClient(path.Replace('\\', '/'));

            bool exists = await blob.ExistsAsync();

            if (!exists)
            {
                return null;
            }
            var properties = await blob.GetPropertiesAsync();

            return new CloudFileInfo(blob.Name, Convert.ToBase64String(properties.Value.ContentHash));
        }

        public async Task<IEnumerable<CloudFileInfo>> GetFilesAsync()
        {
            var blobs = _container.GetBlobsAsync();

            var mapping = blobs
                .SelectAwait(x => new ValueTask<CloudFileInfo>(new CloudFileInfo(x.Name, Convert.ToBase64String(x.Properties.ContentHash))));

            var result = await mapping.ToListAsync();

            return result;
        }

        public async Task WriteFileAsync(string path, Stream file)
        {
            _logger.LogInformation("Writing file: " + path);
            string mimeType = _mimeTypes.GetMimeType(Path.GetExtension(path));

            var blob = _container.GetBlobClient(path);
            await blob.UploadAsync(file, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = mimeType } });

            _logger.LogInformation("{file} saved as {contentType}", path, mimeType);
        }
    }
}
