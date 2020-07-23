using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Uploader
{
    public class DiskContentSource : IContentSource
    {
        private readonly string _basePath;

        public DiskContentSource(DiskContentSourceConfiguration config)
        {
            _basePath = config.BasePath;
        }

        public IReadOnlyCollection<ISourceFileInfo> GetFiles()
        {
            return Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories)
                .Select(x => new DiskSourceFileInfo(new FileInfo(x), _basePath))
                .ToArray();
        }

        public bool HasFile(string relativePath)
        {
            var fullPath = Path.Combine(_basePath, relativePath);

            return File.Exists(fullPath);
        }
    }
}
