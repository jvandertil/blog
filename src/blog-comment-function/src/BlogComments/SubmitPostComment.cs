using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUlid;
using Octokit;
using Octokit.Helpers;

namespace BlogComments
{
    public class SubmitPostComment
    {
        private const int HTTP_NOTFOUND = 404;

        private const string COMMENT_DATA_BASEPATH = "src/blog/data/comments/posts";
        private const string POSTS_BASEPATH = "src/blog/content/posts/";

        private readonly GitHubClientFactory _githubFactory;
        private readonly IOptionsMonitor<GitHubOptions> _options;

        public SubmitPostComment(GitHubClientFactory githubFactory, IOptionsMonitor<GitHubOptions> optionsMonitor)
        {
            _githubFactory = githubFactory;
            _options = optionsMonitor;
        }

        [FunctionName("SubmitPostComment")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{postName}/comment")] HttpRequest req,
            string postName,
            ILogger log)
        {
            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            log.LogInformation("C# HTTP trigger function processed a request.");

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);
            var repository = await github.Repository.Get(username, repositoryName);

            // Verify postName
            var repositoryPostName = await FindPostNameInRepository(github, repository, postName);

            if (repositoryPostName is null)
            {
                return new StatusCodeResult(HTTP_NOTFOUND);
            }

            postName = repositoryPostName;

            var form = await req.ReadFormAsync();

            var userDisplayName = form["name"].Single();
            var commentContent = form["content"].Single();
            var commentId = Ulid.NewUlid().ToString();

            // Create branch
            var branchRef = await github.Git.Reference.CreateBranch(username, repositoryName, "blog-bot/comment/post/" + postName + "/" + commentId);

            string content = SerializeComment(new CommentDto(commentId.ToString(), userDisplayName, DateTimeOffset.UtcNow, commentContent));

            // Create file
            var file = new CreateFileRequest($"Add comment by {userDisplayName} on {postName}", content, branchRef.Ref)
            {
                Committer = new Committer("jvandertil-blog-bot", "noreply@jvandertil.nl", DateTimeOffset.UtcNow),
            };

            await github.Repository.Content.CreateFile(username, repositoryName, COMMENT_DATA_BASEPATH + $"/{postName}/{commentId}.json", file);

            if (settings.EnablePullRequestCreation)
            {
                await github.Repository.PullRequest.Create(username, repositoryName, new NewPullRequest(file.Message, branchRef.Ref, repository.DefaultBranch));
            }

            return new OkObjectResult("OK!");
        }

        private static async Task<string?> FindPostNameInRepository(GitHubClient github, Repository repository, string postName)
        {
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

        private static string SerializeComment(object comment)
        {
            return JsonSerializer.Serialize(comment, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private class CommentDto
        {
            public string Id { get; set; }

            public string DisplayName { get; set; }

            public DateTimeOffset PostedDate { get; set; }

            public string Content { get; set; }

            public CommentDto(string id, string displayName, DateTimeOffset postedDate, string content)
            {
                Id = id;
                DisplayName = displayName;
                PostedDate = postedDate;
                Content = content;
            }
        }
    }
}
