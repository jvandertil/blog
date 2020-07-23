using System.IO;

namespace Uploader
{
    public class DiskSourceFileInfo : ISourceFileInfo
    {
        private readonly FileInfo _fileInfo;

        public string FullName => _fileInfo.FullName;

        public string RelativePath { get; }

        public DiskSourceFileInfo(FileInfo fileInfo, string basePath)
        {
            _fileInfo = fileInfo;

            RelativePath = Path.GetRelativePath(basePath, fileInfo.FullName);
        }

        public Stream OpenRead()
        {
            return _fileInfo.OpenRead();
        }
    }
}
