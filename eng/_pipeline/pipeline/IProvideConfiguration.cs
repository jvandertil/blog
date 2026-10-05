using Fallout.Common;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideConfiguration : IFalloutBuild
    {
        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        Configuration Configuration => TryGetValue(() => Configuration)
                                       ?? (IsLocalBuild ? Configuration.Debug : Configuration.Release);
    }
}
