using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uploader
{
    public class ContentSyncer
    {
        private readonly IContentSource _source;
        private readonly ILogger _logger;
        private readonly IContentDestination _destination;

        public ContentSyncer(IContentSource source, IContentDestination destination, ILogger<ContentSyncer> logger)
        {
            _destination = destination;
            _source = source;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<string>> SynchronizeFilesAsync()
        {
            var processedFiles = new List<string>();
            var sourceFiles = _source.GetFiles();

            foreach (var file in sourceFiles)
            {
                var destinationPath = file.RelativePath.Replace('\\', '/');
                var fileOnDestination = _destination.GetFile(destinationPath);

                if (fileOnDestination is null)
                {
                    // File is missing on destination
                    using var stream = file.OpenRead();

                    await _destination.WriteFileAsync(destinationPath, stream);

                    processedFiles.Add(destinationPath);
                }
                else if (fileOnDestination.ContentMD5 != MD5Hash(file))
                {
                    // File on destination is different
                    await _destination.DeleteFileAsync(destinationPath);

                    using var stream = file.OpenRead();
                    await _destination.WriteFileAsync(destinationPath, stream);

                    processedFiles.Add(destinationPath);
                }
                else
                {
                    _logger.LogTrace("File up to date: {file}", destinationPath);
                }
            }

            var destinationFiles = _destination.GetFiles();
            foreach (var file in destinationFiles)
            {
                if (!_source.HasFile(file.Path))
                {
                    await _destination.DeleteFileAsync(file.Path);
                }
            }

            return processedFiles;
        }

        private string MD5Hash(ISourceFileInfo file)
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenRead();

            return Convert.ToBase64String(md5.ComputeHash(stream));
        }
    }
}
