namespace BlogComments
{
    public class GitHubOptions
    {
        public int ApplicationId { get; set; }

        public string Username { get; set; }

        public string Repository { get; set; }

        public bool EnablePullRequestCreation { get; set; } = false;

        public GitHubOptions()
        {
            Username = null!;
            Repository = null!;
        }
    }
}
