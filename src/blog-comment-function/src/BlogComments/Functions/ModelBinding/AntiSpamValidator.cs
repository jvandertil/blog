using System;
using BlogComments.Functions.Persistence;
using FluentValidation;
using FluentValidation.Validators;

namespace BlogComments.Functions.ModelBinding
{
    public class AntiSpamValidator : PropertyValidator<CommentContents, string>
    {
        private static readonly string[] SpamTerms =
        {
            "mewkid.net",
            "Amoxicillin",
            "azithromycin",
        };

        public override string Name => nameof(AntiSpamValidator);

        public override bool IsValid(ValidationContext<CommentContents> context, string value)
        {
            foreach (var term in SpamTerms)
            {
                if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "Do not post spam.";
        }
    }
}
