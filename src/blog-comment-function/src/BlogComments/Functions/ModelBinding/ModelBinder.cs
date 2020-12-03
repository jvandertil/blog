using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BlogComments.Functions.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace BlogComments.Functions.ModelBinding
{
    public class ModelBinder
    {
        private readonly IValidator<CommentContents> _commentValidator;

        public ModelBinder(IValidator<CommentContents> commentValidator)
        {
            _commentValidator = commentValidator;
        }

        public bool TryBindAndValidate(IFormCollection form,
            [NotNullWhen(true)] out CommentContents? model,
            [NotNullWhen(false)] out IEnumerable<ValidationError>? errors)
        {
            var userDisplayName = form["DisplayName"].FirstOrDefault();
            var commentContent = form["Contents"].FirstOrDefault();

            var boundModel = new CommentContents(userDisplayName, commentContent);
            var errorList = new List<ValidationError>();

            var result = _commentValidator.Validate(boundModel);

            if (result.IsValid)
            {
                model = boundModel;
                errors = null;
                return true;
            }
            else
            {
                model = null;
                errors = result.Errors.Select(x => new ValidationError(x.PropertyName, x.ErrorMessage));

                return false;
            }
        }
    }
}
