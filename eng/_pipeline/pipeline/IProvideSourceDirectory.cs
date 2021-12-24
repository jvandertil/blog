using Nuke.Common;
using Nuke.Common.IO;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideSourceDirectory : INukeBuild
    {
        AbsolutePath SourceDirectory => RootDirectory / "src";
    }
}
