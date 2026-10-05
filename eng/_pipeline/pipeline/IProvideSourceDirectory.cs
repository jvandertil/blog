using Fallout.Common;
using Fallout.Common.IO;

namespace Vandertil.Blog.Pipeline
{
    public interface IProvideSourceDirectory : IFalloutBuild
    {
        AbsolutePath SourceDirectory => RootDirectory / "src";
    }
}
