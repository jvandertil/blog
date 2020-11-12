using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NUlid;

namespace BlogComments.Functions
{
    public class SubmitPostComment
    {
        private const int HTTP_NOTFOUND = 404;
        private const int HTTP_OK = 200;

        private readonly CommentRepository _repository;
        private readonly IPostExistenceChecker _postExistenceChecker;

        public SubmitPostComment(
            CommentRepository repository,
            IPostExistenceChecker postExistenceChecker)
        {
            _repository = repository;
            _postExistenceChecker = postExistenceChecker;
        }

        [FunctionName(nameof(SubmitPostComment))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{postName}/comment")] HttpRequest req,
            [FromRoute] string postName,
            ILogger log)
        {
            // Check if the post actually exists.
            var repositoryPostName = await _postExistenceChecker.TryGetPostFileNameFromRepositoryAsync(postName);
            if (repositoryPostName is null)
            {
                log.LogWarning("Requested post {postName} not found.", postName);
                return new StatusCodeResult(HTTP_NOTFOUND);
            }

            // Use the actual name of the post in the repository (correct casing mostly).
            postName = repositoryPostName;

            // Bind and validate posted form.
            var form = await req.ReadFormAsync();
            if (!TryBindBody(form, out var comment, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

            await _repository.SaveCommentAsync(postName, comment);

            return new StatusCodeResult(HTTP_OK);
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
    }
}
