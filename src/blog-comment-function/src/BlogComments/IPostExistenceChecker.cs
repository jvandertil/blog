using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BlogComments
{
    public interface IPostExistenceChecker
    {
        Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName);
    }

    public class CachingPostExistenceCheckerDecorator : IPostExistenceChecker
    {
        private readonly ConcurrentDictionary<string, string?> _cache;
        private readonly IPostExistenceChecker _decorated;

        public CachingPostExistenceCheckerDecorator(IPostExistenceChecker decorated)
        {
            _cache = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            _decorated = decorated;
        }

        public async Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName)
        {
            string? fileName;

            if (!_cache.TryGetValue(postName, out fileName))
            {
                fileName = await _decorated.TryGetPostFileNameFromRepositoryAsync(postName);

                _cache.TryAdd(postName, fileName);
            }

            return fileName;
        }
    }
}
