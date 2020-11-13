using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlogComments.GitHub;
using Microsoft.Extensions.Options;

namespace BlogComments.Functions.Validation
{
    public class GitHubPostExistenceValidator : IPostExistenceValidator
    {
        private const string POSTS_BASEPATH = "src/blog/content/posts/";

        private readonly GitHubClientFactory _githubFactory;
        private readonly IOptionsMonitor<GitHubOptions> _options;

        public GitHubPostExistenceValidator(GitHubClientFactory githubFactory, IOptionsMonitor<GitHubOptions> options)
        {
            _githubFactory = githubFactory;
            _options = options;
        }

        public async Task<string?> TryGetPostFileNameFromRepositoryAsync(string postName)
        {
            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);
            var repository = await github.Repository.Get(username, repositoryName);

            var tree = await github.Git.Tree.GetRecursive(repository.Id, repository.DefaultBranch);

            var postFileRef = tree.Tree
                .Where(x => x.Path.StartsWith(POSTS_BASEPATH, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(x => x.Path.Contains(postName, StringComparison.OrdinalIgnoreCase));

            if (postFileRef is null)
            {
                return null;
            }

            return Path.GetFileNameWithoutExtension(postFileRef.Path);
        }
    }
}
