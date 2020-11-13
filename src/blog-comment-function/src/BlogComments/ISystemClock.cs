using System;

namespace BlogComments
{
    public interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
