using System.Threading.Tasks;
using BlogComments.Functions.ModelBinding;
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
    public class SubmitPostCommentReply
    {
        private readonly CommentRepository _repository;
        private readonly IPostExistenceValidator _postExistenceValidator;

        public SubmitPostCommentReply(
            CommentRepository repository,
            IPostExistenceValidator postExistenceValidator)
        {
            _repository = repository;
            _postExistenceValidator = postExistenceValidator;
        }

        [FunctionName(nameof(SubmitPostCommentReply))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/posts/{post_name}/comment/{thread_id}/reply/{comment_id}")] HttpRequest req,
            [FromRoute] string post_name,
            [FromRoute] string thread_id,
            [FromRoute] string comment_id,
            ILogger log)
        {
            var repositoryPostName = await _postExistenceValidator.TryGetPostFileNameFromRepositoryAsync(post_name);
            if (repositoryPostName is null)
            {
                return new NotFoundResult();
            }

            var postName = repositoryPostName;

            log.LogInformation("C# HTTP trigger function processed a request.");

            var form = await req.ReadFormAsync();
            if (!Ulid.TryParse(thread_id, out var threadId)
                || !Ulid.TryParse(comment_id, out var commentId))
            {
                return new NotFoundResult();
            }

            if (!ModelBinder.BindAndValidate(form, out var reply, out var errors))
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
