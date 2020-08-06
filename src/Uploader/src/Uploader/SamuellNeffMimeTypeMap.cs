using MimeTypes;

namespace Uploader
{
    public class SamuellNeffMimeTypeMap : IMimeTypeMap
    {
        public string GetMimeType(string extension)
        {
            return MimeTypeMap.GetMimeType(extension);
        }
    }
}
