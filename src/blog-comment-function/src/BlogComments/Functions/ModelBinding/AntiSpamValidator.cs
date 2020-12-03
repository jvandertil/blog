using System;
using FluentValidation.Validators;

namespace BlogComments.Functions.ModelBinding
{
    public class AntiSpamValidator : PropertyValidator
    {
        private static readonly string[] SpamTerms =
        {
            "mewkid.net",
            "Amoxicillin",
        };

        public AntiSpamValidator()
            : base("Do not post spam.")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue is string contents)
            {
                foreach (var term in SpamTerms)
                {
                    if (contents.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
