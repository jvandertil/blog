using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlogComments.Functions.Persistence
{
    public class CommentModel
    {
        public string Id { get; }

        public string DisplayName { get; }

        public DateTimeOffset PostedDate { get; }

        [Required]
        public string Content { get; }

        public bool AuthorComment { get; }

        public IList<CommentModel> Replies { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private CommentModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Replies = new List<CommentModel>();
        }

        public CommentModel(string id, string displayName, DateTimeOffset postedDate, string content)
            : this()
        {
            Id = id;
            DisplayName = displayName;
            PostedDate = postedDate;
            Content = content;

            AuthorComment = false;
        }
    }
}
