using System.ComponentModel.DataAnnotations;

namespace BlogComments.Functions.Persistence
{
    public class CommentContents
    {
        [Required]
        public string DisplayName { get; }

        [Required]
        public string Contents { get; }

        public CommentContents(string displayName, string contents)
        {
            DisplayName = displayName;
            Contents = contents;
        }
    }
}
