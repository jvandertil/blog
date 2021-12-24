using Nuke.Common;
using Nuke.Common.IO;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideArtifactsDirectory : INukeBuild
    {
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    }
}
