using System;
using System.IO;

namespace Uploader.Tests.Stubs
{
    public class StubSourceFile : ISourceFileInfo
    {
        public string RelativePath { get; }

        public byte[] Contents { get; }

        public StubSourceFile(string relativePath)
            : this(relativePath, Array.Empty<byte>())
        {
        }

        public StubSourceFile(string relativePath, byte[] contents)
        {
            RelativePath = relativePath;
            Contents = contents;
        }

        public Stream OpenRead()
        {
            return new MemoryStream(Contents);
        }
    }
}
