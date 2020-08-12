using System;

namespace BlogComments.GitHub
{
    public interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
