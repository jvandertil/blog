using Fallout.Common;
using Fallout.Common.IO;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideArtifactsDirectory : IFalloutBuild
    {
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    }
}
