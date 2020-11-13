using System.Threading.Tasks;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BlogComments.Functions
{
    public class SubmitPostComment
    {
        private readonly CommentRepository _repository;
        private readonly IPostExistenceValidator _postExistenceValidator;

        public SubmitPostComment(
            CommentRepository repository,
            IPostExistenceValidator postExistenceChecker)
        {
            _repository = repository;
            _postExistenceValidator = postExistenceChecker;
        }

        [FunctionName(nameof(SubmitPostComment))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{postName}/comment")] HttpRequest req,
            [FromRoute] string postName,
            ILogger log)
        {
            // Check if the post actually exists.
            var repositoryPostName = await _postExistenceValidator.TryGetPostFileNameFromRepositoryAsync(postName);
            if (repositoryPostName is null)
            {
                log.LogWarning("Requested post {postName} not found.", postName);
                return new NotFoundResult();
            }

            // Use the actual name of the post in the repository (correct casing mostly).
            postName = repositoryPostName;

            // Bind and validate posted form.
            var form = await req.ReadFormAsync();
            if (!ModelBinder.BindAndValidate(form, out var comment, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

            // Save comment
            await _repository.CreateCommentAsync(postName, comment);

            return new OkResult();
        }
    }
}
