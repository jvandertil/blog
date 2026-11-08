using System.Threading.Tasks;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlogComments.Functions
{
    public class SubmitPostComment
    {
        private readonly CommentRepository _repository;
        private readonly IPostExistenceValidator _postExistenceValidator;
        private readonly ITurnstileValidator _turnstileValidator;
        private readonly ModelBinder _modelBinder;
        private readonly ILogger<SubmitPostComment> _logger;

        public SubmitPostComment(
            CommentRepository repository,
            IPostExistenceValidator postExistenceChecker,
            ITurnstileValidator turnstileValidator,
            ModelBinder modelBinder,
            ILogger<SubmitPostComment> logger)
        {
            _repository = repository;
            _postExistenceValidator = postExistenceChecker;
            _turnstileValidator = turnstileValidator;
            _modelBinder = modelBinder;
            _logger = logger;
        }

        [Function(nameof(SubmitPostComment))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{postName}/comment")] HttpRequest req,
            [FromRoute] string postName)
        {
            var form = await req.ReadFormAsync();
            if (!await _turnstileValidator.IsValidAsync(form["cf-turnstile-response"], req.HttpContext.RequestAborted))
            {
                return new BadRequestObjectResult(new[]
                {
                    new ValidationError("cf-turnstile-response", "Please complete the security check.")
                });
            }

            // Check if the post actually exists.
            var repositoryPostName = await _postExistenceValidator.TryGetPostFileNameFromRepositoryAsync(postName);
            if (repositoryPostName is null)
            {
                _logger.LogWarning("Requested post {postName} not found.", postName);
                return new NotFoundResult();
            }

            // Use the actual name of the post in the repository (correct casing mostly).
            postName = repositoryPostName;

            // Bind and validate posted form.
            if (!_modelBinder.TryBindAndValidate(form, out var comment, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

            // Save comment
            await _repository.CreateCommentAsync(postName, comment);

            return new OkResult();
        }
    }
}
