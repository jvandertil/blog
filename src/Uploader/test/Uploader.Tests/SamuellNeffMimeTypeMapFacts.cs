using Xunit;

namespace Uploader.Tests
{
    public class SamuellNeffMimeTypeMapFacts
    {
        private readonly SamuellNeffMimeTypeMap _map;

        public SamuellNeffMimeTypeMapFacts()
        {
            _map = new SamuellNeffMimeTypeMap();
        }

        [Theory]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".htm", "text/html")]
        [InlineData(".html", "text/html")]
        [InlineData(".js", "application/javascript")]
        [InlineData(".css", "text/css")]
        [InlineData(".xml", "text/xml")]
        public void GetMimeType_ReturnsValidMimeTypeForExtension(string extension, string expected)
        {
            var actual = _map.GetMimeType(extension);

            Assert.Equal(expected, actual);
        }
    }
}
