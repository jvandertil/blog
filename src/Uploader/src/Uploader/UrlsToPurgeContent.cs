using System.Collections.Generic;

namespace Uploader
{
    public class UrlsToPurgeContent
    {
        public IEnumerable<string> Files { get; set; }

        // Deserialization constructor
        private UrlsToPurgeContent()
        {
            Files = null!;
        }

        public UrlsToPurgeContent(IEnumerable<string> files)
        {
            Files = files;
        }
    }
}
