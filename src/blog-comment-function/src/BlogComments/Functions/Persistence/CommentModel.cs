using System;
using System.Collections.Generic;

namespace BlogComments.Functions.Persistence
{
    public class CommentModel
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public DateTimeOffset PostedDate { get; set; }

        public string Content { get; set; }

        public bool AuthorComment { get; set; }

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
