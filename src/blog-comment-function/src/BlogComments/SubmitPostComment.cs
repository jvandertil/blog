using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
        private const int HTTP_OK = 200;

        private const string COMMENT_DATA_BASEPATH = "src/blog/data/comments/posts";

        private readonly GitHubClientFactory _githubFactory;
        private readonly IOptionsMonitor<GitHubOptions> _options;
        private readonly IPostExistenceChecker _postExistenceChecker;

        public SubmitPostComment(
            GitHubClientFactory githubFactory,
            IOptionsMonitor<GitHubOptions> optionsMonitor,
            IPostExistenceChecker postExistenceChecker)
        {
            _githubFactory = githubFactory;
            _options = optionsMonitor;
            _postExistenceChecker = postExistenceChecker;
        }

        [FunctionName("SubmitPostComment")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{postName}/comment")] HttpRequest req,
            [FromRoute] string postName,
            ILogger log)
        {
            var repositoryPostName = await _postExistenceChecker.TryGetPostFileNameFromRepositoryAsync(postName);

            if (repositoryPostName is null)
            {
                return new StatusCodeResult(HTTP_NOTFOUND);
            }

            postName = repositoryPostName;

            var settings = _options.CurrentValue;

            var username = settings.Username;
            var repositoryName = settings.Repository;

            log.LogInformation("C# HTTP trigger function processed a request.");

            var github = await _githubFactory.CreateClientAsync(username, repositoryName);
            var repository = await github.Repository.Get(username, repositoryName);

            var form = await req.ReadFormAsync();

            if (!TryBindBody(form, out var comment, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

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
                await github.Repository.PullRequest.Create(username, repositoryName, new NewPullRequest(file.Message, branchRef.Ref, repository.DefaultBranch));
            }

            return new StatusCodeResult(HTTP_OK);
        }

        private static string SerializeComment(object comment)
        {
            return JsonSerializer.Serialize(comment, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private static bool TryBindBody(IFormCollection form,
            [NotNullWhen(true)] out CommentModel? model,
            [NotNullWhen(false)] out IEnumerable<ValidationResult>? errors)
        {
            var userDisplayName = form["DisplayName"].FirstOrDefault();
            var commentContent = form["Content"].FirstOrDefault();

            var commentId = Ulid.NewUlid().ToString();
            var date = DateTimeOffset.UtcNow;

            var boundModel = new CommentModel(commentId.ToString(), userDisplayName, date, commentContent);
            var errorList = new List<ValidationResult>();

            if (Validator.TryValidateObject(boundModel, new ValidationContext(boundModel, null, null), errorList, true))
            {
                model = boundModel;
                errors = null;

                return true;
            }
            else
            {
                model = null;
                errors = errorList;

                return false;
            }
        }

        private class CommentModel
        {
            public string Id { get; set; }

            [Required]
            public string DisplayName { get; set; }

            public DateTimeOffset PostedDate { get; set; }

            [Required]
            public string Content { get; set; }

            public CommentModel(string id, string displayName, DateTimeOffset postedDate, string content)
            {
                Id = id;
                DisplayName = displayName;
                PostedDate = postedDate;
                Content = content;
            }
        }
    }
}
