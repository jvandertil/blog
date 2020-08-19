using System;

namespace BlogComments.GitHub
{
    public sealed class RealSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
