using Nuke.Common;
using Nuke.Common.ValueInjection;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideConfiguration : INukeBuild
    {
        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        Configuration Configuration => ValueInjectionUtility.TryGetValue(() => Configuration)
                                       ?? (IsLocalBuild ? Configuration.Debug : Configuration.Release);
    }
}
