using System;

namespace BlogComments
{
    public sealed class RealSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
