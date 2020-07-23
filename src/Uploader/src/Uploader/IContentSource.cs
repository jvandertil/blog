using System.Collections.Generic;

namespace Uploader
{
    public interface IContentSource
    {
        IReadOnlyCollection<ISourceFileInfo> GetFiles();

        bool HasFile(string relativePath);
    }
}
