using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using MimeTypes;

namespace Uploader
{
    public class AzureContentDestination : IContentDestination
    {
        private readonly CloudBlobContainer _container;
        private readonly IMimeTypeMap _mimeTypes;
        private readonly ILogger _logger;

        public AzureContentDestination(
            AzureConfiguration config,
            IMimeTypeMap mimeTypeMap,
            ILogger<AzureContentDestination> logger)
        {
            _mimeTypes = mimeTypeMap;
            _logger = logger;

            CloudStorageAccount account = CloudStorageAccount.Parse(config.ConnectionString);
            CloudBlobClient serviceClient = account.CreateCloudBlobClient();

            _container = serviceClient.GetContainerReference("$web");
        }

        public async Task DeleteFileAsync(string path)
        {
            _logger.LogInformation("Deleting file: " + path);

            await _container.GetBlobReference(path).DeleteAsync();
        }

        public CloudFileInfo? GetFile(string path)
        {
            var blob = _container.GetBlockBlobReference(path.Replace('\\', '/'));

            if (!blob.Exists())
            {
                return null;
            }

            return new CloudFileInfo(blob.Name, blob.Properties.ContentMD5);
        }

        public IEnumerable<CloudFileInfo> GetFiles()
        {
            var blobs = _container.ListBlobs(useFlatBlobListing: true);

            return blobs
                .OfType<CloudBlockBlob>()
                .Select(x => new CloudFileInfo(x.Name, x.Properties.ContentMD5));
        }

        public async Task WriteFileAsync(string path, Stream file)
        {
            _logger.LogInformation("Writing file: " + path);
            string mimeType = _mimeTypes.GetMimeType(Path.GetExtension(path));

            var blob = _container.GetBlockBlobReference(path);
            await blob.UploadFromStreamAsync(file);

            blob.Properties.ContentType = mimeType;
            await blob.SetPropertiesAsync();

            _logger.LogInformation("{file} saved as {contentType}", path, mimeType);

        }
    }
}
