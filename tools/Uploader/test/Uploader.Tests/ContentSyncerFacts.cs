using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uploader.Tests.Stubs;
using Xunit;
using Xunit.Abstractions;

namespace Uploader.Tests
{
    public class ContentSyncerFacts
    {
        private readonly ILogger<ContentSyncer> _logger;
        private readonly StubSource _source;
        private readonly StubDestination _destination;
        private readonly ContentSyncer _syncer;

        public ContentSyncerFacts(ITestOutputHelper outputHelper)
        {
            _logger = new XunitLogger<ContentSyncer>(outputHelper);
            _source = new StubSource();
            _destination = new StubDestination();
            _syncer = new ContentSyncer(_source, _destination, _logger);
        }


        [Fact]
        public async Task SynchronizeFilesAsync_UploadsFileFromSource_IfItDoesNotExistOnDestination()
        {
            const string path = "testfile";
            _source.AddFile(path);

            await _syncer.SynchronizeFilesAsync();

            ShouldBeUploaded(path);
        }

        [Fact]
        public async Task SynchronizeFilesAsync_ReplacesFile_IfTheDestinationFileHasNoMatchingHash()
        {
            const string path = "testfile";

            _source.AddFile(path);
            _destination.AddFile(path, "");

            await _syncer.SynchronizeFilesAsync();

            ShouldBeOverwritten(path);
        }

        [Fact]
        public async Task SynchronizeFilesAsync_DeletesFile_IfNotExistsOnSource()
        {
            const string path = "testfile";

            _destination.AddFile(path, "");

            await _syncer.SynchronizeFilesAsync();

            ShouldBeDeleted(path);
        }

        private void ShouldBeOverwritten(string fileName)
            => AssertState(fileName, FileState.Overwritten);

        private void ShouldBeDeleted(string fileName)
            => AssertState(fileName, FileState.Deleted);

        private void ShouldBeUploaded(string fileName)
            => AssertState(fileName, FileState.Uploaded);

        private void AssertState(string fileName, FileState expectedState)
        {
            var state = _destination.GetFileState(fileName);

            Assert.Equal(expectedState, state);
        }
    }
}
