using System;
using System.Text.Json;
using System.Threading.Tasks;
using BlogComments.GitHub;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Helpers;

namespace BlogComments.Functions.Persistence
{
    public sealed class CommentRepository
    {
        private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private const string COMMENT_DATA_BASEPATH = "src/blog/data/comments/posts";

        private readonly GitHubClientFactory _githubFactory;
        private readonly IOptionsMonitor<GitHubOptions> _options;

        public CommentRepository(GitHubClientFactory githubFactory, IOptionsMonitor<GitHubOptions> options)
        {
            _githubFactory = githubFactory;
            _options = options;
        }

        public async Task SaveCommentAsync(string postName, CommentModel comment)
        {
            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);

            // Create branch
            var branchRef = await github.Git.Reference.CreateBranch(username, repositoryName, "blog-bot/comment/post/" + postName + "/" + comment.Id);

            string content = SerializeComment(comment);

            // Create file
            var file = new CreateFileRequest($"Add comment by {comment.DisplayName} on {postName}", content, branchRef.Ref)
            {
                Committer = new Committer("jvandertil-blog-bot", "noreply@jvandertil.nl", DateTimeOffset.UtcNow),
            };

            await github.Repository.Content.CreateFile(username, repositoryName, COMMENT_DATA_BASEPATH + $"/{postName}/{comment.Id}.json", file);

            if (settings.EnablePullRequestCreation)
            {
                var repository = await github.Repository.Get(username, repositoryName);

                await github.Repository.PullRequest.Create(username, repositoryName, new NewPullRequest(file.Message, branchRef.Ref, repository.DefaultBranch));
            }
        }

        private static string SerializeComment(object comment)
        {
            return JsonSerializer.Serialize(comment, SERIALIZER_OPTIONS);
        }
    }
}
