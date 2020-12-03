namespace BlogComments.Functions.ModelBinding
{
    public class ValidationError
    {
        public string MemberName { get; }

        public string ErrorMessage { get; }

        public ValidationError(string memberName, string errorMessage)
        {
            MemberName = memberName;
            ErrorMessage = errorMessage;
        }
    }
}
