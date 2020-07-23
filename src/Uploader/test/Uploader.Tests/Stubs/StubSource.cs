using System.Collections.Generic;
using System.Linq;

namespace Uploader.Tests.Stubs
{
    public class StubSource : IContentSource
    {
        private readonly List<StubSourceFile> _files;

        public StubSource()
        {
            _files = new List<StubSourceFile>();
        }

        public IReadOnlyCollection<StubSourceFile> GetFiles()
        {
            return _files;
        }

        public bool HasFile(string relativePath)
        {
            return _files.Any(x => x.RelativePath == relativePath);
        }

        IReadOnlyCollection<ISourceFileInfo> IContentSource.GetFiles()
        {
            return GetFiles();
        }

        public void AddFile(string fileName)
        {
            _files.Add(new StubSourceFile(fileName));
        }
    }
}
