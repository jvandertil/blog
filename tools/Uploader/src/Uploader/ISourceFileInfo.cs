using System.IO;

namespace Uploader
{
    public interface ISourceFileInfo
    {
        string RelativePath { get; }

        Stream OpenRead();
    }
}
