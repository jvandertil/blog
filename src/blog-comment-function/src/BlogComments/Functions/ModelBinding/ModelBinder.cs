using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BlogComments.Functions.Persistence;
using Microsoft.AspNetCore.Http;

namespace BlogComments.Functions.ModelBinding
{
    public static class ModelBinder
    {
        public static bool BindAndValidate(IFormCollection form,
            [NotNullWhen(true)] out CommentContents? model,
            [NotNullWhen(false)] out IEnumerable<ValidationResult>? errors)
        {
            var userDisplayName = form["DisplayName"].FirstOrDefault();
            var commentContent = form["Content"].FirstOrDefault();

            var boundModel = new CommentContents(userDisplayName, commentContent);
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
