using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BlogComments.Functions.Validation
{
    public class CachingPostExistenceValidatorDecorator : IPostExistenceValidator
    {
        private readonly ConcurrentDictionary<string, string?> _cache;
        private readonly IPostExistenceValidator _decorated;

        public CachingPostExistenceValidatorDecorator(IPostExistenceValidator decorated)
        {
            _cache = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            _decorated = decorated;
        }

        public async Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName)
        {
            if (!_cache.TryGetValue(postName, out string? fileName))
            {
                fileName = await _decorated.TryGetPostFileNameFromRepositoryAsync(postName);

                _cache.TryAdd(postName, fileName);
            }

            return fileName;
        }
    }
}
