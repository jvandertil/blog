using BlogComments.Functions.Persistence;
using FluentValidation;

namespace BlogComments.Functions.ModelBinding
{
    public class CommentContentsValidator : AbstractValidator<CommentContents>
    {
        public CommentContentsValidator()
        {
            RuleFor(x => x.DisplayName).NotEmpty();
            RuleFor(x => x.Contents).NotEmpty().SetValidator(new AntiSpamValidator());
        }
    }
}
