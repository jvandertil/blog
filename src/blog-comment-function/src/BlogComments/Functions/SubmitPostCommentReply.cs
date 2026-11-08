using System.Threading.Tasks;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using NUlid;

namespace BlogComments.Functions
{
    public class SubmitPostCommentReply
    {
        private readonly CommentRepository _repository;
        private readonly IPostExistenceValidator _postExistenceValidator;
        private readonly ITurnstileValidator _turnstileValidator;
        private readonly ModelBinder _modelBinder;

        public SubmitPostCommentReply(
            CommentRepository repository,
            IPostExistenceValidator postExistenceValidator,
            ITurnstileValidator turnstileValidator,
            ModelBinder modelBinder)
        {
            _repository = repository;
            _postExistenceValidator = postExistenceValidator;
            _turnstileValidator = turnstileValidator;
            _modelBinder = modelBinder;
        }

        [Function(nameof(SubmitPostCommentReply))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{post_name}/comment/{thread_id}/reply/{comment_id}")] HttpRequest req,
            [FromRoute] string post_name,
            [FromRoute] string thread_id,
            [FromRoute] string comment_id)
        {
            var form = await req.ReadFormAsync();
            if (!await _turnstileValidator.IsValidAsync(form["cf-turnstile-response"], req.HttpContext.RequestAborted))
            {
                return new BadRequestObjectResult(new[]
                {
                    new ValidationError("cf-turnstile-response", "Please complete the security check.")
                });
            }

            var repositoryPostName = await _postExistenceValidator.TryGetPostFileNameFromRepositoryAsync(post_name);
            if (repositoryPostName is null)
            {
                return new NotFoundResult();
            }

            var postName = repositoryPostName;

            if (!Ulid.TryParse(thread_id, out var threadId)
                || !Ulid.TryParse(comment_id, out var commentId))
            {
                return new NotFoundResult();
            }

            if (!_modelBinder.TryBindAndValidate(form, out var reply, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

            var success = await _repository.TryAddReplyToThread(postName, threadId, commentId, reply);

            if (!success)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
