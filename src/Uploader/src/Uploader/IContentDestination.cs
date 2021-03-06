using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Uploader
{
    public interface IContentDestination
    {
        IAsyncEnumerable<CloudFileInfo> GetFilesAsync();

        Task<CloudFileInfo?> GetFileAsync(string path);

        Task WriteFileAsync(string path, Stream file);

        Task DeleteFileAsync(string path);
    }
}
