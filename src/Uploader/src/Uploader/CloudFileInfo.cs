namespace Uploader
{
    public class CloudFileInfo
    {
        public string Path { get; }

        public string ContentMD5 { get; }

        public CloudFileInfo(string path, string contentMD5)
        {
            Path = path;
            ContentMD5 = contentMD5;
        }
    }
}
