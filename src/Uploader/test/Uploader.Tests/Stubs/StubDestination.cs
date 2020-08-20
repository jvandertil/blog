using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Uploader.Tests.Stubs
{
    public enum FileState
    {
        None = 0,
        Uploaded = 1,
        Deleted = 2,
        Overwritten = 3,
    }

    public class StubDestination : IContentDestination
    {
        private readonly Dictionary<string, FileState> _fileActions;

        private readonly List<CloudFileInfo> _content;

        public StubDestination()
        {
            _fileActions = new Dictionary<string, FileState>();

            _content = new List<CloudFileInfo>();
        }

        public FileState GetFileState(string path)
        {
            return _fileActions[path];
        }

        public Task DeleteFileAsync(string path)
        {
            _fileActions[path] = FileState.Deleted;

            _content.Remove(_content.Single(x => x.Path == path));

            return Task.CompletedTask;
        }

        public Task<CloudFileInfo> GetFileAsync(string path)
        {
            return Task.FromResult(_content.SingleOrDefault(x => x.Path == path));
        }

        public IAsyncEnumerable<CloudFileInfo> GetFilesAsync()
        {
            return _content.ToList().ToAsyncEnumerable();
        }

        public Task WriteFileAsync(string path, Stream file)
        {
            if (_fileActions.TryGetValue(path, out var state))
            {
                if (state == FileState.Deleted)
                {
                    _fileActions[path] = FileState.Overwritten;
                }
                else
                {
                    throw new InvalidOperationException("Overwrite of file detected without delete.");
                }
            }
            else
            {
                _fileActions[path] = FileState.Uploaded;
            }

            _content.Add(new CloudFileInfo(path, ""));

            return Task.CompletedTask;
        }

        public void AddFile(string fileName, string md5Hash)
        {
            _content.Add(new CloudFileInfo(fileName, md5Hash));
        }
    }
}
