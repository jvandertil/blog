using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlogComments.GitHub;
using Microsoft.Extensions.Options;
using NUlid;
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
        private readonly ISystemClock _clock;

        public CommentRepository(
            GitHubClientFactory githubFactory,
            IOptionsMonitor<GitHubOptions> options,
            ISystemClock clock)
        {
            _githubFactory = githubFactory;
            _options = options;
            _clock = clock;
        }

        public async Task<bool> TryAddReplyToThread(string postName, Ulid threadId, Ulid commentId, CommentContents contents)
        {
            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);
            var repository = await github.Repository.Get(username, repositoryName);

            // Find thread in comments
            var targetFile = COMMENT_DATA_BASEPATH + $"/{postName}/{threadId}.json";

            // Load and deserialize comment
            var existingFileResult = await github.Repository.Content.GetAllContentsByRef(username, repositoryName, targetFile, repository.DefaultBranch);
            var existingFile = existingFileResult.SingleOrDefault();

            if (existingFile is null)
            {
                return false;
            }

            var comment = DeserializeComment(existingFile.Content);

            var reply = MapComment(contents);

            if (!AddReplyToCommentRecursive(comment, commentId.ToString(), reply))
            {
                return false;
            }

            var branchRef = await github.Git.Reference.CreateBranch(username, repositoryName, "blog-bot/comment/post/" + postName + "/" + threadId + "/" + reply.Id);

            var fileContent = SerializeComment(comment);
            var file = new UpdateFileRequest($"Add reply by {reply.DisplayName} on {postName}, thread {threadId}", fileContent, existingFile.Sha, branchRef.Ref)
            {
                Committer = new Committer("jvandertil-blog-bot", "noreply@jvandertil.nl", DateTimeOffset.UtcNow),
            };

            await github.Repository.Content.UpdateFile(username, repositoryName, targetFile, file);

            if (settings.EnablePullRequestCreation)
            {
                await github.Repository.PullRequest.Create(username, repositoryName, new NewPullRequest(file.Message, branchRef.Ref, repository.DefaultBranch));
            }

            return true;

            static bool AddReplyToCommentRecursive(CommentModel comment, string commentId, CommentModel reply)
            {
                if (comment.Id == commentId)
                {
                    comment.Replies.Add(reply);
                    return true;
                }

                foreach (var entry in comment.Replies)
                {
                    if (AddReplyToCommentRecursive(entry, commentId, reply))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public async Task CreateCommentAsync(string postName, CommentContents contents)
        {
            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);

            var comment = MapComment(contents);

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

        private CommentModel MapComment(CommentContents contents)
        {
            var commentId = Ulid.NewUlid().ToString();
            var date = _clock.UtcNow;

            return new CommentModel(commentId, contents.DisplayName, date, contents.Contents);
        }

        private static CommentModel DeserializeComment(string content)
            => JsonSerializer.Deserialize<CommentModel>(content, SERIALIZER_OPTIONS);

        private static string SerializeComment(CommentModel comment)
            => JsonSerializer.Serialize(comment, SERIALIZER_OPTIONS);
    }
}
