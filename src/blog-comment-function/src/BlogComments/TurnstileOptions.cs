namespace BlogComments
{
    public class TurnstileOptions
    {
        public string SecretKey { get; set; } = null!;
        public string[] ExpectedHostnames { get; set; } = System.Array.Empty<string>();
    }
}
